using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class BackgroundMqServices(R2VirtualFiles r2) : Service
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

    public Task Any(DbWrites request) => Request.ExecuteCommandsAsync(request);
    
    public Task Any(AiServerTasks request) => Request.ExecuteCommandsAsync(request);

    public async Task Any(AnalyticsTasks request)
    {
        if (request.CreatePostStat == null && request.CreateSearchStat == null && request.DeletePost == null)
            return;

        using var analyticsDb = HostContext.AppHost.GetDbConnection(Databases.Analytics);
        
        if (request.CreatePostStat != null)// && !Stats.IsAdminOrModerator(request.RecordPostView.UserName))
        {
            await analyticsDb.InsertAsync(request.CreatePostStat);
        }

        if (request.CreateSearchStat != null)// && !Stats.IsAdminOrModerator(request.RecordSearchView.UserName))
        {
            await analyticsDb.InsertAsync(request.CreateSearchStat);
        }

        if (request.DeletePost != null)
        {
            await analyticsDb.DeleteAsync<PostStat>(x => x.PostId == request.DeletePost);
        }
    }
}
