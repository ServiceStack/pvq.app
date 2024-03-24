using System.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MyApp.Components.Shared;
using MyApp.Data;
using MyApp.ServiceInterface;
using MyApp.ServiceModel;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.IO;
using ServiceStack.OrmLite;

[assembly: HostingStartup(typeof(MyApp.ConfigureRenderer))]

namespace MyApp;

public class ConfigureRenderer : IHostingStartup
{
    static T RequiredService<T>() where T : notnull => 
        ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<T>();

    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context,services) =>
        {
            var svc = new ServiceCollection();
            svc.AddLogging();
            svc.AddSingleton<AppConfig>(c => RequiredService<AppConfig>());
            svc.AddSingleton<MarkdownQuestions>(c => RequiredService<MarkdownQuestions>());
            svc.AddSingleton<NavigationManager>(c => new StaticNavigationManager());
            var sp = svc.BuildServiceProvider();
            services.AddScoped<HtmlRenderer>(c => new HtmlRenderer(sp, c.GetRequiredService<ILoggerFactory>()));
            
            services.AddScoped<BlazorRenderer>();
            services.AddSingleton<RendererCache>();
            services.RegisterService<RenderServices>();
        })
        .ConfigureAppHost(appHost => { });
}

// Use fake NavigationManager in Static Rendering to avoid NavigationManager has not been initialized Exception
internal class StaticNavigationManager : NavigationManager
{
    public StaticNavigationManager()
    {
        Initialize("https://pvq.app/", "https://pvq.app/");
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        NotifyLocationChanged(false);
    }
}

public class RendererCache(AppConfig appConfig, R2VirtualFiles r2)
{
    private static bool DisableCache = false;

    public string GetCachedQuestionPostPath(int id) => appConfig.CacheDir.CombineWith(GetQuestionPostVirtualPath(id));

    public string GetQuestionPostVirtualPath(int id)
    {
        var idParts = id.ToFileParts();
        var fileName = $"{idParts.fileId}.{nameof(QuestionPost)}.html";
        var dirPath = $"{idParts.dir1}/{idParts.dir2}";
        var filePath = $"{dirPath}/{fileName}";
        return filePath;
    }

    public async Task<string?> GetQuestionPostHtmlAsync(int id)
    {
        if (DisableCache)
            return null;
        var filePath = GetCachedQuestionPostPath(id);
        if (File.Exists(filePath))
            return await File.ReadAllTextAsync(filePath);
        return null;
    }

    public async Task SetQuestionPostHtmlAsync(int id, string html)
    {
        if (DisableCache)
            return;
        var (dir1, dir2, fileId) = id.ToFileParts();
        appConfig.CacheDir.CombineWith($"{dir1}/{dir2}").AssertDir();
        var filePath = GetCachedQuestionPostPath(id);
        await File.WriteAllTextAsync(filePath, html);
    }

    private string GetHtmlTabFilePath(string? tab)
    {
        var partialName = string.IsNullOrEmpty(tab)
            ? ""
            : $".{tab}";
        var filePath = appConfig.CacheDir.CombineWith($"HomeTab{partialName}.html");
        return filePath;
    }

    static TimeSpan HomeTabValidDuration = TimeSpan.FromMinutes(5);

    public async Task SetHomeTabHtmlAsync(string? tab, string html)
    {
        if (DisableCache)
            return;
        appConfig.CacheDir.AssertDir();
        var filePath = GetHtmlTabFilePath(tab);
        await File.WriteAllTextAsync(filePath, html);
    }

    public async Task<string?> GetHomeTabHtmlAsync(string? tab)
    {
        if (DisableCache)
            return null;
        var filePath = GetHtmlTabFilePath(tab);
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Exists)
        {
            if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc > HomeTabValidDuration)
                return null;

            var html = await fileInfo.ReadAllTextAsync();
            if (!string.IsNullOrEmpty(html))
                return html;
        }

        return null;
    }
}

public class RenderServices(
    QuestionsProvider questions,
    BlazorRenderer renderer,
    RendererCache cache,
    IDbConnectionFactory dbFactory,
    MemoryCacheClient memory) : Service
{
    public async Task Any(RenderComponent request)
    {
        if (request.IfQuestionModified != null || request.RegenerateMeta != null)
        {
            // Runs at most once per minute per post
            var id = request.IfQuestionModified.GetValueOrDefault(request.RegenerateMeta ?? 0);

            // Whether to rerender the Post HTML
            var localFiles = questions.GetLocalQuestionFiles(id);
            var remoteFiles = await questions.GetRemoteQuestionFilesAsync(id);
            var dbStatTotals = await Db.SelectAsync<StatTotals>(x => x.PostId == id);

            using var dbAnalytics = await dbFactory.OpenAsync(Databases.Analytics);
            var allPostVotes = await Db.SelectAsync<Vote>(x => x.PostId == id);

            var regenerateMeta = request.RegenerateMeta != null || 
                                 await ShouldRegenerateMeta(id, localFiles, remoteFiles, dbStatTotals, allPostVotes);
            if (regenerateMeta)
            {
                await RegenerateMeta(dbAnalytics, id, remoteFiles, dbStatTotals, allPostVotes);
            }
            
            var rerenderPostHtml = regenerateMeta;
            var htmlPostPath = cache.GetCachedQuestionPostPath(id);
            var htmlPostFile = new FileInfo(htmlPostPath);
            if (!rerenderPostHtml && htmlPostFile.Exists)
            {
                // If any question files have modified since the last rendered HTML
                rerenderPostHtml = localFiles.Files.FirstOrDefault()?.LastModified > htmlPostFile.LastWriteTime;
            }

            if (rerenderPostHtml)
            {
                request.Question = await localFiles.GetQuestionAsync();
            }
        }

        if (request.Question != null)
        {
            var html = await renderer.RenderComponent<QuestionPost>(new() { ["Question"] = request.Question });
            await cache.SetQuestionPostHtmlAsync(request.Question.Id, html);
        }

        if (request.Home != null)
        {
            var html = await renderer.RenderComponent<HomeTab>(new()
            {
                ["Tab"] = request.Home.Tab,
                ["Posts"] = request.Home.Posts,
            });
            await cache.SetHomeTabHtmlAsync(request.Home.Tab, html);
        }
    }

    public async Task<bool> ShouldRegenerateMeta(
        int id,
        QuestionFiles localFiles,
        QuestionFiles remoteFiles,
        List<StatTotals> dbStatTotals,
        List<Vote> allPostVotes)
    {
        var localMetaFile = localFiles.GetMetaFile();
        var remoteMetaFile = remoteFiles.GetMetaFile();
        var postId = $"{id}";
        var dbPostStatTotals = dbStatTotals.FirstOrDefault(x => x.Id == postId);

        // Whether to recalculate and rerender the meta.json
        var recalculateMeta = localMetaFile == null || remoteMetaFile == null ||
                              // 1min Intervals + R2 writes take longer 
                              localMetaFile.LastModified < remoteMetaFile.LastModified.ToUniversalTime().AddSeconds(-30);

        var livePostUpVotes = allPostVotes.Count(x => x.RefId == postId && x.Score > 0);
        var livePostDownVotes = allPostVotes.Count(x => x.RefId == postId && x.Score > 0);

        recalculateMeta = recalculateMeta
            || dbPostStatTotals == null
            || dbPostStatTotals.UpVotes != dbPostStatTotals.StartingUpVotes + livePostUpVotes 
            || dbPostStatTotals.DownVotes != livePostDownVotes;
        // postStatTotals.ViewCount != totalPostViews // ViewCount shouldn't trigger a regeneration

        if (!recalculateMeta)
        {
            var jsonMeta = (await localMetaFile!.ReadAllTextAsync()).FromJson<Meta>();
            var jsonStatTotals = jsonMeta.StatTotals ?? [];
            var jsonPostStatTotals = jsonStatTotals.FirstOrDefault(x => x.Id == postId);

            var answerCount = remoteFiles.GetAnswerFilesCount();

            recalculateMeta = (1 + answerCount) > dbStatTotals.Count || dbStatTotals.Count > jsonStatTotals.Count
                || dbPostStatTotals?.Matches(jsonPostStatTotals) != true 
                || dbStatTotals.Sum(x => x.UpVotes) != jsonStatTotals.Sum(x => x.UpVotes)
                || dbStatTotals.Sum(x => x.DownVotes) != jsonStatTotals.Sum(x => x.DownVotes)
                || dbStatTotals.Sum(x => x.StartingUpVotes) != jsonStatTotals.Sum(x => x.StartingUpVotes);
        }
        return recalculateMeta;
    }
    
    public async Task RegenerateMeta(IDbConnection dbAnalytics, int id, QuestionFiles remoteFiles, 
        List<StatTotals> dbStatTotals, List<Vote> allPostVotes)
    {
        var now = DateTime.Now;
        var remoteMetaFile = remoteFiles.GetMetaFile();
        var postId = $"{id}";

        Meta meta;
        if (remoteMetaFile != null)
        {
            meta = QuestionFiles.DeserializeMeta(await remoteMetaFile.ReadAllTextAsync());
        }
        else
        {
            meta = new() {};
        }
        foreach (var answerFile in remoteFiles.GetAnswerFiles())
        {
            var model = remoteFiles.GetAnswerUserName(answerFile.Name);
            if (!meta.ModelVotes.ContainsKey(model))
                meta.ModelVotes[model] = QuestionFiles.ModelScores.GetValueOrDefault(model, 0);
        }
        if (meta.Id == default)
            meta.Id = id;
        meta.ModifiedDate = now;

        var dbPost = await Db.SingleByIdAsync<Post>(id);
        var totalPostViews = dbAnalytics.Count<PostView>(x => x.PostId == id);
        var livePostUpVotes = allPostVotes.Count(x => x.RefId == postId && x.Score > 0);
        var livePostDownVotes = allPostVotes.Count(x => x.RefId == postId && x.Score < 0);
        var liveStats = new List<StatTotals>
        {
            new()
            {
                Id = postId,
                PostId = id,
                ViewCount = (int)totalPostViews,
                FavoriteCount = dbPost?.FavoriteCount ?? 0,
                StartingUpVotes = dbPost?.Score ?? 0,
                UpVotes = livePostUpVotes,
                DownVotes = livePostDownVotes,
            },
        };
        foreach (var answerFile in remoteFiles.GetAnswerFiles())
        {
            var answerId = remoteFiles.GetAnswerId(answerFile.Name);
            var answerModel = remoteFiles.GetAnswerUserName(answerFile.Name);
            var answer = answerFile.Name.Contains(".h.")
                ? (await answerFile.ReadAllTextAsync()).FromJson<Post>()
                : null;
            var answerStats = new StatTotals
            {
                Id = answerId,
                PostId = id,
                UpVotes = allPostVotes.Count(x => x.RefId == answerId && x.Score > 0),
                DownVotes = allPostVotes.Count(x => x.RefId == answerId && x.Score < 0),
                StartingUpVotes = answer?.Score ?? meta.ModelVotes.GetValueOrDefault(answerModel, 0),
            };
            liveStats.Add(answerStats);
        }
        foreach (var liveStat in liveStats)
        {
            var dbStat = dbStatTotals.FirstOrDefault(x => x.Id == liveStat.Id);
            if (dbStat == null)
            {
                await Db.InsertAsync(liveStat);
            }
            else
            {
                await Db.UpdateOnlyAsync(() => new StatTotals
                {
                    Id = liveStat.Id,
                    PostId = liveStat.PostId,
                    ViewCount = liveStat.ViewCount,
                    FavoriteCount = liveStat.FavoriteCount,
                    UpVotes = liveStat.UpVotes,
                    DownVotes = liveStat.DownVotes,
                    StartingUpVotes = liveStat.StartingUpVotes,
                }, x => x.Id == liveStat.Id);
            }
        }
                
        meta.StatTotals = await Db.SelectAsync<StatTotals>(x => x.PostId == id);
        await questions.WriteMetaAsync(meta);
    }
}