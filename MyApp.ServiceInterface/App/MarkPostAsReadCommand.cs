using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Worker(Databases.App)]
[Tag(Tags.Notifications)]
public class MarkPostAsReadCommand(AppConfig appConfig, IDbConnection db) : SyncCommand<MarkPostAsRead>
{
    protected override void Run(MarkPostAsRead request)
    {
        db.UpdateOnly(() => new Notification { Read = true }, 
            where:x => x.UserName == request.UserName && x.PostId == request.PostId);
        appConfig.ResetUnreadNotificationsFor(db, request.UserName);
    }
}
