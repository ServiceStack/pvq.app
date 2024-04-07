using System.Data;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class DeletePostCommand(AppConfig appConfig, IDbConnection db) : IAsyncCommand<DeletePost>
{
    public async Task ExecuteAsync(DeletePost request)
    {
        foreach (var postId in request.Ids)
        {
            await db.DeleteAsync<PostJob>(x => x.PostId == postId);
            await db.DeleteAsync<Vote>(x => x.PostId == postId);
            await db.DeleteByIdAsync<Post>(postId);
            appConfig.ResetInitialPostId(db);
        }
    }
}
