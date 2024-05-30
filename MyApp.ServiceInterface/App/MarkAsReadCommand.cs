using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Notifications)]
public class MarkAsReadCommand(AppConfig appConfig, IDbConnection db) : IAsyncCommand<MarkAsRead>
{
    public async Task ExecuteAsync(MarkAsRead request)
    {
        var userName = request.UserName;
        if (request.AllNotifications == true)
        {
            await db.UpdateOnlyAsync(() => new Notification { Read = true }, x => x.UserName == userName);
            appConfig.UsersUnreadNotifications[userName] = 0;
        }
        else if (request.NotificationIds?.Count > 0)
        {
            await db.UpdateOnlyAsync(() => new Notification { Read = true }, 
                x => x.UserName == userName && request.NotificationIds.Contains(x.Id));
            appConfig.UsersUnreadNotifications[userName] = (int) await db.CountAsync(
                db.From<Notification>().Where(x => x.UserName == userName && !x.Read));
        }
        // Mark all achievements as read isn't used, they're auto reset after viewed
        if (request.AllAchievements == true)
        {
            await db.UpdateOnlyAsync(() => new Achievement { Read = true }, x => x.UserName == userName);
            appConfig.UsersUnreadAchievements[userName] = 0;
        }
        else if (request.AchievementIds?.Count > 0)
        {
            await db.UpdateOnlyAsync(() => new Achievement { Read = true }, 
                x => x.UserName == userName && request.AchievementIds.Contains(x.Id));
            appConfig.UsersUnreadAchievements[userName] = (int) await db.CountAsync(
                db.From<Achievement>().Where(x => x.UserName == userName && !x.Read));
        }
    }
}
