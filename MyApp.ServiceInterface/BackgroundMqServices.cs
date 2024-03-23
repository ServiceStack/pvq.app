using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class BackgroundMqServices(R2VirtualFiles r2) : Service
{
    public async Task Any(DiskTasks request)
    {
        if (request.SaveFile != null)
        {
            await r2.WriteFileAsync(request.SaveFile.FilePath, request.SaveFile.Stream);
        }

        if (request.CdnDeleteFiles != null)
        {
            r2.DeleteFiles(request.CdnDeleteFiles);
        }
    }

    public async Task Any(DbWriteTasks request)
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
