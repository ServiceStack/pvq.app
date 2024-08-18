using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.Jobs;

namespace MyApp.ServiceInterface;

public class CdnServices(R2VirtualFiles r2, IBackgroundJobs jobs) : Service
{
    public object Any(DeleteCdnFilesMq request)
    {
        var arg = new DiskTasks
        {
            CdnDeleteFiles = request.Files
        };
        jobs.RunCommand<DiskTasksCommand>(arg);
        return arg;
    }

    public void Any(DeleteCdnFile request)
    {
        r2.DeleteFile(request.File);
    }

    public object Any(GetCdnFile request)
    {
        var file = r2.GetFile(request.File);
        if (file == null)
            throw new FileNotFoundException(request.File);
        return new HttpResult(file);
    }
}
