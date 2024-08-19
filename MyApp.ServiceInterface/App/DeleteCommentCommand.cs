using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Database)]
[Worker(Databases.App)]
public class DeleteCommentCommand(AppConfig appConfig, IDbConnection db) : SyncCommand<DeleteComment>
{
    protected override void Run(DeleteComment request)
    {
        var refId = $"{request.Id}-{request.Created}";
        var rowsAffected = db.Delete(db.From<Notification>()
            .Where(x => x.RefId == refId && x.RefUserName == request.CreatedBy));
        if (rowsAffected > 0)
        {
            appConfig.ResetUsersUnreadNotifications(db);
        }
    }
}
