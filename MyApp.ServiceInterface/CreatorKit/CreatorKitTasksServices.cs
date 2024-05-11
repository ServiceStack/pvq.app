using CreatorKit.ServiceModel;
using ServiceStack;
using MyApp.Data;
using MyApp.ServiceInterface;

namespace CreatorKit.ServiceInterface;

public class CreatorKitTasksServices : Service
{
    public object Any(CreatorKitTasks request) => Request.ExecuteCommandsAsync(request);
}