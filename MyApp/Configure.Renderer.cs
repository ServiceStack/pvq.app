using System.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MyApp.Components.Shared;
using MyApp.Data;
using MyApp.ServiceInterface;
using MyApp.ServiceModel;
using ServiceStack.Caching;
using ServiceStack.Data;
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

public class RenderServices(
    ILogger<RenderServices> log,
    QuestionsProvider questions,
    BlazorRenderer renderer,
    RendererCache cache,
    IDbConnectionFactory dbFactory,
    AppConfig appConfig,
    MarkdownQuestions markdown) : Service
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
                log.LogInformation("Regenerating Meta for Post {Id}...", id);
                await RegenerateMeta(dbAnalytics, id, remoteFiles, dbStatTotals, allPostVotes);
                
                // TODO improve
                MessageProducer.Publish(new DbWrites {
                    UpdateReputations = true
                });
            }

            // Update Local Files with new or modified remote files
            foreach (var remoteFile in remoteFiles.Files)
            {
                var localFile = localFiles.Files.FirstOrDefault(x => x.Name == remoteFile.Name);
                if (localFile == null || localFile.Length != remoteFile.Length)
                {
                    log.LogInformation("Saving local file for {State} {Path}", localFile == null ? "new" : "modified", remoteFile.VirtualPath);
                    var remoteContents = await remoteFile.ReadAllTextAsync();
                    await questions.SaveLocalFileAsync(remoteFile.VirtualPath, remoteContents);
                }
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
            log.LogInformation("Rendering Question Post HTML {Id}...", request.Question.Id);
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

        var answerFiles = remoteFiles.GetAnswerFiles().ToList();
        foreach (var answerFile in answerFiles)
        {
            var model = remoteFiles.GetAnswerUserName(answerFile.Name);
            if (!meta.ModelVotes.ContainsKey(model))
                meta.ModelVotes[model] = QuestionsProvider.ModelScores.GetValueOrDefault(model, 0);
        }
        if (meta.Id == default)
            meta.Id = id;
        meta.ModifiedDate = now;

        var dbPost = await Db.SingleByIdAsync<Post>(id);
        if (dbPost.AnswerCount != answerFiles.Count)
        {
            await Db.UpdateOnlyAsync(() => new Post { AnswerCount = answerFiles.Count }, x => x.Id == id);
        }
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
        foreach (var answerFile in answerFiles)
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
    
    public object Any(PreviewMarkdown request)
    {
        var html = markdown.GenerateHtml(request.Markdown);
        return html;
    }
}
