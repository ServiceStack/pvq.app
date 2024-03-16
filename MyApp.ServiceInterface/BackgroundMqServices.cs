using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.IO;

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
}