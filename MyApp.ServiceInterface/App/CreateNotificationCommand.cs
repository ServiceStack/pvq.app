using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Notifications)]
public class CreateNotificationCommand(AppConfig appConfig, IDbConnection db) : IAsyncCommand<Notification>
{
    public async Task ExecuteAsync(Notification request)
    {
        await db.InsertAsync(request);
        appConfig.IncrUnreadNotificationsFor(request.UserName);
    }
}