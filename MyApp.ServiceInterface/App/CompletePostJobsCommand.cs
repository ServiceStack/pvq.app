using System.Data;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack.Messaging;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class CompletePostJobsCommand(IDbConnection Db, ModelWorkerQueue modelWorkers, IMessageProducer mqClient) : IExecuteCommandAsync<CompletePostJobs>
{
    public async Task ExecuteAsync(CompletePostJobs request)
    {
        var jobIds = request.Ids;
        await Db.UpdateOnlyAsync(() => new PostJob {
                CompletedDate = DateTime.UtcNow,
            }, 
            x => jobIds.Contains(x.Id));
        var postJobs = await Db.SelectAsync(Db.From<PostJob>()
            .Where(x => jobIds.Contains(x.Id)));

        foreach (var postJob in postJobs)
        {
            // If there's no outstanding model answer jobs for this post, add a rank job
            if (!await Db.ExistsAsync(Db.From<PostJob>()
                    .Where(x => x.PostId == postJob.PostId && x.CompletedDate == null)))
            {
                var rankJob = new PostJob
                {
                    PostId = postJob.PostId,
                    Model = "rank",
                    Title = postJob.Title,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = nameof(DbWrites),
                };
                await Db.InsertAsync(rankJob);
                modelWorkers.Enqueue(rankJob);
                mqClient.Publish(new SearchTasks { AddPostToIndex = postJob.PostId });
            }
        }
    }
}