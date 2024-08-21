using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Notifications)]
[Worker(Databases.App)]
public class MarkAsReadCommand(AppConfig appConfig, IDbConnection db) : SyncCommand<MarkAsRead>
{
    protected override void Run(MarkAsRead request)
    {
        var userName = Request.GetClaimsPrincipal().GetRequiredUserName();
        if (request.AllNotifications == true)
        {
            db.UpdateOnly(() => new Notification { Read = true }, x => x.UserName == userName);
            appConfig.UsersUnreadNotifications[userName] = 0;
        }
        else if (request.NotificationIds?.Count > 0)
        {
            db.UpdateOnly(() => new Notification { Read = true }, 
                x => x.UserName == userName && request.NotificationIds.Contains(x.Id));
            appConfig.UsersUnreadNotifications[userName] = (int) db.Count(
                db.From<Notification>().Where(x => x.UserName == userName && !x.Read));
        }
        // Mark all achievements as read isn't used, they're auto reset after viewed
        if (request.AllAchievements == true)
        {
            db.UpdateOnly(() => new Achievement { Read = true }, x => x.UserName == userName);
            appConfig.UsersUnreadAchievements[userName] = 0;
        }
        else if (request.AchievementIds?.Count > 0)
        {
            db.UpdateOnly(() => new Achievement { Read = true }, 
                x => x.UserName == userName && request.AchievementIds.Contains(x.Id));
            appConfig.UsersUnreadAchievements[userName] = (int) db.Count(
                db.From<Achievement>().Where(x => x.UserName == userName && !x.Read));
        }
    }
}
