using System.Data;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack.Jobs;

namespace MyApp.ServiceInterface.App;

[Worker(Databases.App)]
[Tag(Tags.Answers)]
public class DeleteAnswersCommand(ILogger<DeleteAnswersCommand> logger, IBackgroundJobs jobs, IDbConnection db) 
    : SyncCommand<DeleteAnswers>
{
    protected override void Run(DeleteAnswers request)
    {
        var log = Request.CreateJobLogger(jobs,logger);
        foreach (var refId in request.Ids)
        {
            try
            {
                log.LogInformation("Deleting answer {RefId}...", refId);

                db.Delete<Vote>(x => x.RefId == refId);
                db.DeleteById<Post>(refId);
                db.Delete<StatTotals>(x => x.Id == refId);
                db.Delete<Notification>(x => x.RefId == refId);
                db.Delete<PostEmail>(x => x.RefId == refId);
            }
            catch (Exception e)
            {
                log.LogError(e, "Failed to delete Answer {RefId}: {Message}", refId, e.Message);
            }
        }
    }
}
