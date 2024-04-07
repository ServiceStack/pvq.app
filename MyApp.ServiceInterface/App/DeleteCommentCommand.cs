using System.Data;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class DeleteCommentCommand(AppConfig appConfig, IDbConnection db) : IAsyncCommand<DeleteComment>
{
    public async Task ExecuteAsync(DeleteComment request)
    {
        var refId = $"{request.Id}-{request.Created}";
        var rowsAffected = await db.DeleteAsync(db.From<Notification>()
            .Where(x => x.RefId == refId && x.RefUserName == request.CreatedBy));
        if (rowsAffected > 0)
        {
            appConfig.ResetUsersUnreadNotifications(db);
        }
    }
}
