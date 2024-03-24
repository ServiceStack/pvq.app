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
            Db.BulkInsert(request.CreatePostJobs, new() { Mode = BulkInsertMode.Sql });
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
            });
        }
    }

    public async Task Any(AnalyticsTasks request)
    {
        if (request.RecordPostView != null && !Stats.IsAdminOrModerator(request.RecordPostView.UserName))
        {
            using var analyticsDb = HostContext.AppHost.GetDbConnection(Databases.Analytics);
            await analyticsDb.InsertAsync(request.RecordPostView);
        }

        if (request.RecordSearchView != null && !Stats.IsAdminOrModerator(request.RecordSearchView.UserName))
        {
            using var analyticsDb = HostContext.AppHost.GetDbConnection(Databases.Analytics);
            await analyticsDb.InsertAsync(request.RecordSearchView);
        }
    }
}
