using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.IO;

namespace MyApp.ServiceInterface;

public class CdnServices(R2VirtualFiles r2) : Service
{
    public object Any(DeleteCdnFilesMq request)
    {
        var msg = new DiskTasks
        {
            CdnDeleteFiles = request.Files
        };
        PublishMessage(msg);
        return msg;
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
