using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;

namespace MyApp.ServiceInterface;

public class AdminServices(AppConfig appConfig) : Service
{
    public async Task<object> Any(Sync request)
    {
        if (request.Tasks != null)
        {
            var db = Db;
            var tasks = request.Tasks;
            if (tasks.Contains(nameof(AppConfig.ResetInitialPostId)))
                appConfig.ResetInitialPostId(db);
            if (tasks.Contains(nameof(AppConfig.ResetUsersReputation)))
                appConfig.ResetUsersReputation(db);
            if (tasks.Contains(nameof(AppConfig.ResetUsersQuestions)))
                appConfig.ResetUsersQuestions(db);
            if (tasks.Contains(nameof(AppConfig.ResetUsersUnreadAchievements)))
                appConfig.ResetUsersUnreadAchievements(db);
            if (tasks.Contains(nameof(AppConfig.ResetUsersUnreadNotifications)))
                appConfig.ResetUsersUnreadNotifications(db);
            if (tasks.Contains(nameof(AppConfig.UpdateUsersReputation)))
                appConfig.UpdateUsersReputation(db);
            if (tasks.Contains(nameof(AppConfig.UpdateUsersQuestions)))
                appConfig.UpdateUsersQuestions(db);
        }
        return new StringResponse { Result = "OK" };
    }
}