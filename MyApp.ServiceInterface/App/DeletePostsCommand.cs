using System.Data;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack.Jobs;

namespace MyApp.ServiceInterface.App;

[Worker(Databases.App)]
[Tag(Tags.Database)]
public class DeletePostsCommand(ILogger<DeleteAnswersCommand> logger, IBackgroundJobs jobs, 
    AppConfig appConfig, IDbConnection db) 
    : SyncCommand<DeletePosts>
{
    protected override void Run(DeletePosts request)
    {
        var log = Request.CreateJobLogger(jobs,logger);
        foreach (var postId in request.Ids)
        {
            try
            {
                log.LogInformation("Deleting Question {Id}...", postId);

                db.Delete<Vote>(x => x.PostId == postId);
                db.DeleteById<Post>(postId);
                db.Delete<StatTotals>(x => x.PostId == postId);
                db.Delete<Notification>(x => x.PostId == postId);
                db.Delete<WatchPost>(x => x.PostId == postId);
                db.Delete<PostEmail>(x => x.PostId == postId);
                appConfig.ResetInitialPostId(db);
            }
            catch (Exception e)
            {
                log.LogError(e, "Failed to delete Question {Id}: {Message}", postId, e.Message);
            }
        }
    }
}
