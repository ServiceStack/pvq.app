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

    public async Task Any(AnalyticsTasks request)
    {
        if (request.RecordPostStat != null && !Stats.IsAdminOrModerator(request.RecordPostStat.UserName))
        {
            using var analyticsDb = HostContext.AppHost.GetDbConnection(Databases.Analytics);
            await analyticsDb.InsertAsync(request.RecordPostStat);
        }

        if (request.RecordSearchStat != null && !Stats.IsAdminOrModerator(request.RecordSearchStat.UserName))
        {
            using var analyticsDb = HostContext.AppHost.GetDbConnection(Databases.Analytics);
            await analyticsDb.InsertAsync(request.RecordSearchStat);
        }
    }
}
