using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Database)]
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
