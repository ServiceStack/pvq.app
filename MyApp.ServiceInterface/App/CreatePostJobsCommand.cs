using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;

namespace MyApp.ServiceInterface.App;

public class CreatePostJobsCommand(IDbConnection db, ModelWorkerQueue modelWorkers) : IAsyncCommand<CreatePostJobs>
{
    public async Task ExecuteAsync(CreatePostJobs request)
    {
        await db.SaveAllAsync(request.PostJobs);
        request.PostJobs.ForEach(modelWorkers.Enqueue);
    }
}