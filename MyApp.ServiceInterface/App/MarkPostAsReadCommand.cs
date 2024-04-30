using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

public class MarkPostAsReadCommand(AppConfig appConfig, IDbConnection db, QuestionsProvider questions) : IAsyncCommand<MarkPostAsRead>
{
    public async Task ExecuteAsync(MarkPostAsRead request)
    {
        await db.UpdateOnlyAsync(() => new Notification { Read = true }, 
            where:x => x.UserName == request.UserName && x.PostId == request.PostId);
        await appConfig.ResetUnreadNotificationsForAsync(db, request.UserName);
    }
}