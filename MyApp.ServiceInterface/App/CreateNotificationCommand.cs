using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Notifications)]
[Worker(Databases.App)]
public class CreateNotificationCommand(AppConfig appConfig, IDbConnection db) : SyncCommand<Notification>
{
    protected override void Run(Notification request)
    {
        db.Insert(request);
        appConfig.IncrUnreadNotificationsFor(request.UserName);
    }
}
