using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Database)]
[Worker(Databases.App)]
public class DeleteCommentCommand(AppConfig appConfig, IDbConnection db) : AsyncCommand<DeleteComment>
{
    protected override async Task RunAsync(DeleteComment request, CancellationToken token)
    {
        var refId = $"{request.Id}-{request.Created}";
        var rowsAffected = await db.DeleteAsync(db.From<Notification>()
            .Where(x => x.RefId == refId && x.RefUserName == request.CreatedBy), token: token);
        if (rowsAffected > 0)
        {
            appConfig.ResetUsersUnreadNotifications(db);
        }
    }
}
