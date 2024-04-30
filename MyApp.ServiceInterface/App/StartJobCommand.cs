using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

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
