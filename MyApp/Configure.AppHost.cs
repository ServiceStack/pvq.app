using System.Data;
using System.Diagnostics;
using Amazon.S3;
using Microsoft.AspNetCore.Mvc.Rendering;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceInterface;
using MyApp.ServiceModel;
using ServiceStack.Data;
using ServiceStack.Text;

[assembly: HostingStartup(typeof(MyApp.AppHost))]

namespace MyApp;

public class AppHost() : AppHostBase("MyApp"), IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context,services) => {
            // Configure ASP.NET Core IOC Dependencies
            context.Configuration.GetSection(nameof(AppConfig)).Bind(AppConfig.Instance);
            services.AddSingleton(AppConfig.Instance);

            services.AddSingleton<ImageCreator>();
            
            var r2AccountId = context.Configuration.GetValue("R2AccountId", Environment.GetEnvironmentVariable("R2_ACCOUNT_ID"));
            var r2AccessId = context.Configuration.GetValue("R2AccessKeyId", Environment.GetEnvironmentVariable("R2_ACCESS_KEY_ID"));
            var r2AccessKey = context.Configuration.GetValue("R2SecretAccessKey", Environment.GetEnvironmentVariable("R2_SECRET_ACCESS_KEY"));
            var s3Client = new AmazonS3Client(r2AccessId, r2AccessKey, new AmazonS3Config
            {
                ServiceURL = $"https://{r2AccountId}.r2.cloudflarestorage.com"
            });
            services.AddSingleton(s3Client);
            var appFs = new R2VirtualFiles(s3Client, "stackoverflow-shootout");
            services.AddSingleton(appFs);
            
            var questionsDir = context.HostingEnvironment.ContentRootPath.CombineWith("App_Data/questions");
#if DEBUG
            questionsDir = Path.GetFullPath(
                Path.Combine(context.HostingEnvironment.ContentRootPath, "../../pvq/questions"));
#endif
            services.AddSingleton(c => new QuestionsProvider(
                c.GetRequiredService<ILogger<QuestionsProvider>>(),
                new FileSystemVirtualFiles(questionsDir),
                appFs));

            services.AddPlugin(new FilesUploadFeature(
                new UploadLocation("profiles", appFs, allowExtensions: FileExt.WebImages,
                    // Use unique URL to invalidate CDN caches
                    resolvePath: ctx =>
                    {
                        var userName = ctx.Session.UserName;
                        return $"/profiles/{userName[..2]}/{userName}/{ctx.FileName}";
                    },
                    maxFileBytes: ImageUtils.MaxAvatarSize,
                    transformFile: ImageUtils.TransformAvatarAsync)
            ));
        });

    public override void Configure()
    {
        AppConfig.Instance.GitPagesBaseUrl ??= ResolveGitBlobBaseUrl(ContentRootDirectory);
        
        FileSystemVirtualFiles.AssertDirectory(HostingEnvironment.ContentRootPath.CombineWith(AppConfig.Instance.CacheDir));
        FileSystemVirtualFiles.AssertDirectory(HostingEnvironment.ContentRootPath.CombineWith(AppConfig.Instance.ProfilesDir));

        using var db = GetDbConnection();
        AppConfig.Instance.Init(db);
        
        AppConfig.Instance.LoadTags(new FileInfo(Path.Combine(HostingEnvironment.WebRootPath, "data/tags.txt")));
        Log.Info($"Loaded {AppConfig.Instance.AllTags.Count} tags");

        var incompleteJobs = db.Select(db.From<PostJob>().Where(x => x.CompletedDate == null));
        if (incompleteJobs.Count > 0)
        {
            var modelWorkers = base.ApplicationServices.GetRequiredService<ModelWorkerQueue>();
            incompleteJobs.ForEach(modelWorkers.Enqueue);
        }
    }
    
    private string? ResolveGitBlobBaseUrl(IVirtualDirectory contentDir)
    {
        var srcDir = new DirectoryInfo(contentDir.RealPath);
        var gitConfig = new FileInfo(Path.Combine(srcDir.Parent!.FullName, ".git", "config"));
        if (gitConfig.Exists)
        {
            var txt = gitConfig.ReadAllText();
            var pos = txt.IndexOf("url = ", StringComparison.Ordinal);
            if (pos >= 0)
            {
                var url = txt[(pos + "url = ".Length)..].LeftPart(".git").LeftPart('\n').Trim();
                var gitBaseUrl = url.CombineWith($"blob/main/{srcDir.Name}");
                return gitBaseUrl;
            }
        }
        return null;
    }

    public async Task PrerenderSitemapAsync(ServiceStackHost appHost, string distDir, string baseUrl)
    {
        var log = appHost.Resolve<ILogger<SitemapFeature>>();
        log.LogInformation("Prerendering Sitemap...");
        var sw = Stopwatch.StartNew();

        using var db = await appHost.Resolve<IDbConnectionFactory>().OpenDbConnectionAsync();
        var feature = await CreateSitemapAsync(log, db, baseUrl);
        await feature.RenderToAsync(distDir);

        log.LogInformation("Sitemap took {Elapsed} to prerender", sw.Elapsed.Humanize());
    }

    async Task<SitemapFeature> CreateSitemapAsync(ILogger log, IDbConnection db, string baseUrl)
    {
        var now = DateTime.UtcNow;

        DateTime NValidDate(DateTime? date) => date == null
            ? new DateTime(now.Year, now.Month, 1)
            : ValidDate(date.Value);
        DateTime ValidDate(DateTime date) =>
            date.Year < 2000 ? new DateTime(now.Year, date.Month, date.Day) : date;

        var posts = await db.SelectAsync(db.From<Post>());
        var page = 1;
        var batches = posts.BatchesOf(200);

        var urlSet = new List<SitemapUrl>();
        urlSet.AddRange([
            new() { Location = "/questions/ask" },
            new() { Location = "/leaderboard" },
            new() { Location = "/blog" },
            new() { Location = "/posts" },
            new() { Location = "/posts/pvq-intro" },
            //new() { Location = "/posts/leaderboard-intro" }, 20/04/2024
            new() { Location = "/about" },
        ]);
        urlSet.ForEach(x =>
        {
            x.LastModified ??= now;
            x.ChangeFrequency ??= SitemapFrequency.Weekly;
            if (x.Location.StartsWith('/'))
                x.Location = baseUrl.CombineWith(x.Location);
        });

        var tags = new HashSet<string>();
        foreach (var post in posts)
        {
            foreach (var tag in post.Tags.Safe())
            {
                tags.Add(tag);
            }
        }

        var tagsSiteMap = new Sitemap
        {
            Location = baseUrl.CombineWith("/sitemaps/sitemap-tags.xml"),
            AtPath = "/sitemaps/sitemap-tags.xml",
            LastModified = now,
            UrlSet = tags.Select(x => new SitemapUrl
            {
                Location = baseUrl.CombineWith($"/questions/tagged/{x.UrlEncode()}"),
                LastModified = now,
                ChangeFrequency = SitemapFrequency.Weekly,
            }).ToList()
        };
        
        var to = new SitemapFeature {
            SitemapIndex =
            {
                new Sitemap
                {
                    Location = baseUrl.CombineWith("/sitemaps/sitemap.xml"),
                    AtPath = "/sitemaps/sitemap.xml",
                    LastModified = now,
                    UrlSet = urlSet
                },
                new Sitemap
                {
                    Location = baseUrl.CombineWith("/sitemaps/sitemap-questions.xml"),
                    AtPath = "/sitemaps/sitemap-questions.xml",
                    LastModified = ValidDate(posts.Max(x => x.LastEditDate)!.Value),
                    UrlSet = batches.Select(batch => new SitemapUrl {
                        Location = baseUrl.CombineWith($"/questions?tab=newest&page={page++}&pagesize=200"),
                        LastModified = NValidDate(batch.Max(x => x.LastEditDate)),
                        ChangeFrequency = SitemapFrequency.Weekly,
                    }).ToList()
                },
                tagsSiteMap,
            }
        };
        log.LogInformation("Sitemap Batches: {Count}", page);

        return to;
    }

    
}

public static class HtmlHelpers
{
    public static string ToAbsoluteContentUrl(string? relativePath) => HostContext.DebugMode 
        ? AppConfig.Instance.LocalBaseUrl.CombineWith(relativePath)
        : AppConfig.Instance.PublicBaseUrl.CombineWith(relativePath);
    public static string ToAbsoluteApiUrl(string? relativePath) => HostContext.DebugMode 
        ? AppConfig.Instance.LocalBaseUrl.CombineWith(relativePath)
        : AppConfig.Instance.PublicBaseUrl.CombineWith(relativePath);

    public static string ContentUrl(this IHtmlHelper html, string? relativePath) => ToAbsoluteContentUrl(relativePath); 
    public static string ApiUrl(this IHtmlHelper html, string? relativePath) => ToAbsoluteApiUrl(relativePath);
}
