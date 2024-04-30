using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

public class FailJobCommand(IDbConnection db, ModelWorkerQueue modelWorkers) : IAsyncCommand<FailJob>
{
    public async Task ExecuteAsync(FailJob request)
    {
        await db.UpdateAddAsync(() => new PostJob {
                Error = request.Error,
                RetryCount = 1,
            }, 
            x => x.PostId == request.Id);
        var postJob = await db.SingleByIdAsync<PostJob>(request.Id);
        if (postJob != null)
        {
            if (postJob.RetryCount > 3)
            {
                await db.UpdateOnlyAsync(() =>
                        new PostJob { CompletedDate = DateTime.UtcNow },
                    x => x.PostId == request.Id);
            }
            else
            {
                modelWorkers.Enqueue(postJob);
            }
        }
    }
}
