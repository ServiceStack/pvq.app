using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Notifications)]
[Worker(Databases.App)]
public class CreateNotificationCommand(AppConfig appConfig, IDbConnection db) : AsyncCommand<Notification>
{
    protected override async Task RunAsync(Notification request, CancellationToken token)
    {
        await db.InsertAsync(request, token: token);
        appConfig.IncrUnreadNotificationsFor(request.UserName);
    }
}
