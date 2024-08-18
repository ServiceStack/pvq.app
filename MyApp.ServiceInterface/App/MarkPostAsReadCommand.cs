using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Worker(Databases.App)]
[Tag(Tags.Notifications)]
public class MarkPostAsReadCommand(AppConfig appConfig, IDbConnection db, QuestionsProvider questions) : AsyncCommand<MarkPostAsRead>
{
    protected override async Task RunAsync(MarkPostAsRead request, CancellationToken token)
    {
        await db.UpdateOnlyAsync(() => new Notification { Read = true }, 
            where:x => x.UserName == request.UserName && x.PostId == request.PostId, token:token);
        await appConfig.ResetUnreadNotificationsForAsync(db, request.UserName);
    }
}
