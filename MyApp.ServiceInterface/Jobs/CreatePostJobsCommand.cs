using System.Data;
using MyApp.Data;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.Jobs;

[Tag(Tags.Jobs)]
public class CreatePostJobsCommand(IDbConnection db, ModelWorkerQueue modelWorkers) : IAsyncCommand<CreatePostJobs>
{
    public async Task ExecuteAsync(CreatePostJobs request)
    {
        await db.SaveAllAsync(request.PostJobs);
        request.PostJobs.ForEach(modelWorkers.Enqueue);
    }
}