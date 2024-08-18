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
    : AsyncCommand<StatTotals>
{
    protected override async Task RunAsync(StatTotals request, CancellationToken token)
    {
        var lastUpdated = request.LastUpdated ?? DateTime.UtcNow;
        appConfig.SetLastUpdated(request.Id, lastUpdated);
        var updatedRow = await db.UpdateOnlyAsync(() => new StatTotals
        {
            StartingUpVotes = request.StartingUpVotes,
            CreatedBy = request.CreatedBy,
            LastUpdated = lastUpdated,
        }, x => x.Id == request.Id, token: token);
        
        if (updatedRow == 0)
        {
            await db.InsertAsync(request, token:token);
        }

        if (request.CreatedBy != null)
        {
            answerNotifier.NotifyNewAnswer(request.PostId, request.CreatedBy);
        }

        jobs.RunCommand<RegenerateMetaCommand>(new RegenerateMeta { ForPost = request.PostId });
    }
}
