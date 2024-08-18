using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Worker(Databases.App)]
[Tag(Tags.Notifications)]
public class MarkAsReadCommand(AppConfig appConfig, IDbConnection db) : AsyncCommand<MarkAsRead>
{
    protected override async Task RunAsync(MarkAsRead request, CancellationToken token)
    {
        var userName = request.UserName;
        if (request.AllNotifications == true)
        {
            await db.UpdateOnlyAsync(() => new Notification { Read = true }, x => x.UserName == userName, token: token);
            appConfig.UsersUnreadNotifications[userName] = 0;
        }
        else if (request.NotificationIds?.Count > 0)
        {
            await db.UpdateOnlyAsync(() => new Notification { Read = true }, 
                x => x.UserName == userName && request.NotificationIds.Contains(x.Id), token: token);
            appConfig.UsersUnreadNotifications[userName] = (int) await db.CountAsync(
                db.From<Notification>().Where(x => x.UserName == userName && !x.Read), token: token);
        }
        // Mark all achievements as read isn't used, they're auto reset after viewed
        if (request.AllAchievements == true)
        {
            await db.UpdateOnlyAsync(() => new Achievement { Read = true }, x => x.UserName == userName, token: token);
            appConfig.UsersUnreadAchievements[userName] = 0;
        }
        else if (request.AchievementIds?.Count > 0)
        {
            await db.UpdateOnlyAsync(() => new Achievement { Read = true }, 
                x => x.UserName == userName && request.AchievementIds.Contains(x.Id), token: token);
            appConfig.UsersUnreadAchievements[userName] = (int) await db.CountAsync(
                db.From<Achievement>().Where(x => x.UserName == userName && !x.Read), token: token);
        }
    }
}
