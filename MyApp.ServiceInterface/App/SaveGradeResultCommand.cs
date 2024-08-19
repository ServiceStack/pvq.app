using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceInterface.Renderers;
using MyApp.ServiceModel;
using ServiceStack.Jobs;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Answers)]
[Worker(Databases.App)]
public class SaveGradeResultCommand(AppConfig appConfig, IDbConnection db, WorkerAnswerNotifier answerNotifier, IBackgroundJobs jobs) 
    : SyncCommand<StatTotals>
{
    protected override void Run(StatTotals request)
    {
        var lastUpdated = request.LastUpdated ?? DateTime.UtcNow;
        appConfig.SetLastUpdated(request.Id, lastUpdated);
        var updatedRow = db.UpdateOnly(() => new StatTotals
        {
            StartingUpVotes = request.StartingUpVotes,
            CreatedBy = request.CreatedBy,
            LastUpdated = lastUpdated,
        }, x => x.Id == request.Id);
        
        if (updatedRow == 0)
        {
            db.Insert(request);
        }

        if (request.CreatedBy != null)
        {
            answerNotifier.NotifyNewAnswer(request.PostId, request.CreatedBy);
        }

        jobs.RunCommand<RegenerateMetaCommand>(new RegenerateMeta { ForPost = request.PostId });
    }
}
