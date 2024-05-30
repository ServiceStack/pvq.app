using System.Data;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.Jobs;

[Tag(Tags.Jobs)]
public class StartJobCommand(IDbConnection db) : IAsyncCommand<StartJob>
{
    public async Task ExecuteAsync(StartJob job)
    {
        await db.UpdateOnlyAsync(() => new PostJob
        {
            StartedDate = DateTime.UtcNow,
            Worker = job.Worker,
            WorkerIp = job.WorkerIp,
        }, x => x.PostId == job.Id);
    }
}
