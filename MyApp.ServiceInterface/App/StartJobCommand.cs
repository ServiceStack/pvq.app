using System.Data;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class StartJobCommand(IDbConnection db) : IExecuteCommandAsync<StartJob>
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
