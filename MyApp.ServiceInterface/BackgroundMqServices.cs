using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class BackgroundMqServices(R2VirtualFiles r2, ModelWorkerQueue modelWorkers, QuestionsProvider questions) : Service
{
    public async Task Any(DiskTasks request)
    {
        var saveFile = request.SaveFile;
        if (saveFile != null)
        {
            if (saveFile.Stream != null)
            {
                await r2.WriteFileAsync(saveFile.FilePath, saveFile.Stream);
            }
            else if (saveFile.Text != null)
            {
                await r2.WriteFileAsync(saveFile.FilePath, saveFile.Text);
            }
            else if (saveFile.Bytes != null)
            {
                await r2.WriteFileAsync(saveFile.FilePath, saveFile.Bytes);
            }
        }

        if (request.CdnDeleteFiles != null)
        {
            r2.DeleteFiles(request.CdnDeleteFiles);
        }
    }

    public async Task Any(DbWrites request)
    {
        var vote = request.RecordPostVote;
        if (vote != null)
        {
            if (string.IsNullOrEmpty(vote.RefId))
                throw new ArgumentNullException(nameof(vote.RefId));
            if (string.IsNullOrEmpty(vote.UserName))
                throw new ArgumentNullException(nameof(vote.UserName));

            await Db.DeleteAsync<Vote>(new { vote.RefId, vote.UserName });
            if (vote.Score != 0)
            {
                await Db.InsertAsync(vote);
            }
            
            MessageProducer.Publish(new RenderComponent {
                RegenerateMeta = vote.PostId
            });
        }

        if (request.CreatePost != null)
        {
            await Db.InsertAsync(request.CreatePost);
        }
        
        if (request.CreatePostJobs is { Count: > 0 })
        {
            await Db.SaveAllAsync(request.CreatePostJobs);
            request.CreatePostJobs.ForEach(modelWorkers.Enqueue);
        }

        var startJob = request.StartJob;
        if (startJob != null)
        {
            await Db.UpdateOnlyAsync(() => new PostJob
            {
                StartedDate = DateTime.UtcNow,
                Worker = startJob.Worker,
                WorkerIp = startJob.WorkerIp,
            }, x => x.PostId == startJob.Id);
        }

        if (request.CompleteJobIds is { Count: > 0 })
        {
            await Db.UpdateOnlyAsync(() => new PostJob {
                    CompletedDate = DateTime.UtcNow,
                }, 
                x => request.CompleteJobIds.Contains(x.Id));
            var postJobs = await Db.SelectAsync(Db.From<PostJob>()
                .Where(x => request.CompleteJobIds.Contains(x.Id)));

            foreach (var postJob in postJobs)
            {
                // If there's no outstanding model answer jobs for this post, add a rank job
                if (!Db.Exists(Db.From<PostJob>()
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
                }
            }
        }

        if (request.FailJob != null)
        {
            await Db.UpdateOnlyAsync(() => new PostJob {
                    CompletedDate = DateTime.UtcNow,
                    Error = request.FailJob.Error,
                }, 
                x => x.PostId == request.FailJob.Id);
        }
        
        if (request.AnswerAddedToPost != null)
        {
            await Db.UpdateAddAsync(() => new Post
            {
                AnswerCount = 1,
            }, x => x.Id == request.AnswerAddedToPost.Value);
        }
    }

    public async Task Any(AnalyticsTasks request)
    {
        if (request.RecordPostView != null)// && !Stats.IsAdminOrModerator(request.RecordPostView.UserName))
        {
            using var analyticsDb = HostContext.AppHost.GetDbConnection(Databases.Analytics);
            await analyticsDb.InsertAsync(request.RecordPostView);
        }

        if (request.RecordSearchView != null)// && !Stats.IsAdminOrModerator(request.RecordSearchView.UserName))
        {
            using var analyticsDb = HostContext.AppHost.GetDbConnection(Databases.Analytics);
            await analyticsDb.InsertAsync(request.RecordSearchView);
        }
    }
}
