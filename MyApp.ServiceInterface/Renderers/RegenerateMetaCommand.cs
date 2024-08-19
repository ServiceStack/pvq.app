using System.Data;
using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceInterface.App;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.Renderers;

public class RegenerateMeta
{
    public int? IfPostModified { get; set; }
    public int? ForPost { get; set; }
}

[Tag(Tags.Renderer)]
public class RegenerateMetaCommand(
    ILogger<RegenerateMetaCommand> logger,
    IBackgroundJobs jobs,
    IDbConnectionFactory dbFactory,
    QuestionsProvider questions,
    RendererCache cache)
    : AsyncCommandWithResult<RegenerateMeta, QuestionAndAnswers?>
{
    protected override async Task<QuestionAndAnswers?> RunAsync(RegenerateMeta question, CancellationToken token)
    {
        var id = question.IfPostModified.GetValueOrDefault(question.ForPost ?? 0);
        if (id < 0)
            throw new ArgumentNullException(nameof(id));

        var log = Request.CreateJobLogger(jobs, logger);
        // Whether to rerender the Post HTML
        using var db = dbFactory.Open();
        var localFiles = questions.GetLocalQuestionFiles(id);
        var remoteFiles = await questions.GetRemoteQuestionFilesAsync(id);
        var dbStatTotals = db.Select<StatTotals>(x => x.PostId == id);

        using var dbAnalytics = dbFactory.Open(Databases.Analytics);
        var allPostVotes = db.Select<Vote>(x => x.PostId == id);

        var regenerateMeta = question.ForPost != null ||
            await ShouldRegenerateMeta(id, localFiles, remoteFiles, dbStatTotals, allPostVotes, token);
        if (regenerateMeta)
        {
            log.LogInformation("Regenerating Meta for Post {Id}...", id);
            await RegenerateMeta(log, db, dbAnalytics, id, remoteFiles, dbStatTotals, allPostVotes, token);
            jobs.RunCommand<UpdateReputationsCommand>();
        }

        // Update Local Files with new or modified remote files
        foreach (var remoteFile in remoteFiles.Files)
        {
            var localFile = localFiles.Files.FirstOrDefault(x => x.Name == remoteFile.Name);
            if (localFile == null || localFile.Length != remoteFile.Length)
            {
                log.LogInformation("Saving local file for {State} {Path}", localFile == null ? "new" : "modified",
                    remoteFile.VirtualPath);
                var remoteContents = await remoteFile.ReadAllTextAsync(token);
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
            var result = await localFiles.GetQuestionAsync();
            if (result != null)
            {
                jobs.RunCommand("RenderQuestionPostCommand", result);
            }
            return result;
        }
        return null;
    }

    public async Task<bool> ShouldRegenerateMeta(
        int id,
        QuestionFiles localFiles,
        QuestionFiles remoteFiles,
        List<StatTotals> dbStatTotals,
        List<Vote> allPostVotes,
        CancellationToken token)
    {
        var localMetaFile = localFiles.GetMetaFile();
        var remoteMetaFile = remoteFiles.GetMetaFile();
        var postId = $"{id}";
        var dbPostStatTotals = dbStatTotals.FirstOrDefault(x => x.Id == postId);

        // Whether to recalculate and rerender the meta.json
        var recalculateMeta = localMetaFile == null || remoteMetaFile == null ||
                              // 1min Intervals + R2 writes take longer 
                              localMetaFile.LastModified <
                              remoteMetaFile.LastModified.ToUniversalTime().AddSeconds(-30);

        var livePostUpVotes = allPostVotes.Count(x => x.RefId == postId && x.Score > 0);
        var livePostDownVotes = allPostVotes.Count(x => x.RefId == postId && x.Score > 0);

        recalculateMeta = recalculateMeta
                          || dbPostStatTotals == null
                          || dbPostStatTotals.UpVotes != dbPostStatTotals.StartingUpVotes + livePostUpVotes
                          || dbPostStatTotals.DownVotes != livePostDownVotes;
        // postStatTotals.ViewCount != totalPostViews // ViewCount shouldn't trigger a regeneration

        if (!recalculateMeta)
        {
            var jsonMeta = (await localMetaFile!.ReadAllTextAsync(token)).FromJson<Meta>();
            var jsonStatTotals = jsonMeta.StatTotals ?? [];
            var jsonPostStatTotals = jsonStatTotals.FirstOrDefault(x => x.Id == postId);

            var answerCount = remoteFiles.GetAnswerFilesCount();

            recalculateMeta = (1 + answerCount) > dbStatTotals.Count
                              || dbStatTotals.Count > jsonStatTotals.Count
                              || dbPostStatTotals?.Matches(jsonPostStatTotals) != true
                              || dbStatTotals.Sum(x => x.UpVotes) != jsonStatTotals.Sum(x => x.UpVotes)
                              || dbStatTotals.Sum(x => x.DownVotes) != jsonStatTotals.Sum(x => x.DownVotes)
                              || dbStatTotals.Sum(x => x.StartingUpVotes) != jsonStatTotals.Sum(x => x.StartingUpVotes);
        }

        return recalculateMeta;
    }

    public async Task RegenerateMeta(JobLogger log, IDbConnection db, IDbConnection dbAnalytics, int id, QuestionFiles remoteFiles,
        List<StatTotals> dbStatTotals, List<Vote> allPostVotes, CancellationToken token)
    {
        var now = DateTime.Now;
        var remoteMetaFile = remoteFiles.GetMetaFile();
        var postId = $"{id}";

        Meta meta;
        if (remoteMetaFile != null)
        {
            meta = QuestionFiles.DeserializeMeta(await remoteMetaFile.ReadAllTextAsync(token));
        }
        else
        {
            meta = new() { };
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

        var dbPost = db.SingleById<Post>(id);
        if (dbPost == null)
        {
            log.LogWarning("Post {Id} not found", id);
            return;
        }
        if (dbPost.AnswerCount != answerFiles.Count)
        {
            db.UpdateOnly(() => new Post { AnswerCount = answerFiles.Count }, 
                x => x.Id == id);
        }

        var totalPostViews = dbAnalytics.Count<PostStat>(x => x.PostId == id);
        var livePostUpVotes = allPostVotes.Count(x => x.RefId == postId && x.Score > 0);
        var livePostDownVotes = allPostVotes.Count(x => x.RefId == postId && x.Score < 0);
        var liveStats = new List<StatTotals>
        {
            new()
            {
                Id = postId,
                PostId = id,
                ViewCount = (int)totalPostViews,
                FavoriteCount = dbPost.FavoriteCount ?? 0,
                StartingUpVotes = dbPost.Score,
                UpVotes = livePostUpVotes,
                DownVotes = livePostDownVotes,
                CreatedBy = dbPost.CreatedBy,
            },
        };
        foreach (var answerFile in answerFiles)
        {
            var answerId = remoteFiles.GetAnswerId(answerFile.Name);
            var answerModel = remoteFiles.GetAnswerUserName(answerFile.Name);
            var answerStats = new StatTotals
            {
                Id = answerId,
                PostId = id,
                UpVotes = allPostVotes.Count(x => x.RefId == answerId && x.Score > 0),
                DownVotes = allPostVotes.Count(x => x.RefId == answerId && x.Score < 0),
                StartingUpVotes = meta.ModelVotes.GetValueOrDefault(answerModel, 0),
                CreatedBy = answerModel,
            };
            liveStats.Add(answerStats);
        }

        foreach (var liveStat in liveStats)
        {
            var dbStat = dbStatTotals.FirstOrDefault(x => x.Id == liveStat.Id);
            if (dbStat == null)
            {
                db.Insert(liveStat);
            }
            else
            {
                db.UpdateOnly(() => new StatTotals
                {
                    Id = liveStat.Id,
                    PostId = liveStat.PostId,
                    ViewCount = liveStat.ViewCount,
                    FavoriteCount = liveStat.FavoriteCount,
                    UpVotes = liveStat.UpVotes,
                    DownVotes = liveStat.DownVotes,
                    StartingUpVotes = liveStat.StartingUpVotes,
                    CreatedBy = liveStat.CreatedBy,
                }, x => x.Id == liveStat.Id);
            }
        }

        meta.StatTotals = db.Select<StatTotals>(x => x.PostId == id);
        await questions.WriteMetaAsync(meta);
    }
}
