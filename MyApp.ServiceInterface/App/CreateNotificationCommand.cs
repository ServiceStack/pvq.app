using System.Data;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class CreateNotificationCommand(AppConfig appConfig, IDbConnection db) : IExecuteCommandAsync<Notification>
{
    public async Task ExecuteAsync(Notification request)
    {
        await db.InsertAsync(request);
        appConfig.IncrUnreadNotificationsFor(request.UserName);
    }
}