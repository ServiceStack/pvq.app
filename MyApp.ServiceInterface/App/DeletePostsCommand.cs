using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Worker(Databases.App)]
[Tag(Tags.Database)]
public class DeletePostsCommand(AppConfig appConfig, IDbConnection db) 
    : AsyncCommand<DeletePosts>
{
    protected override async Task RunAsync(DeletePosts request, CancellationToken token)
    {
        foreach (var postId in request.Ids)
        {
            await db.DeleteAsync<Vote>(x => x.PostId == postId, token: token);
            await db.DeleteByIdAsync<Post>(postId, token: token);
            await db.DeleteAsync<StatTotals>(x => x.PostId == postId, token: token);
            await db.DeleteAsync<Notification>(x => x.PostId == postId, token: token);
            await db.DeleteAsync<WatchPost>(x => x.PostId == postId, token: token);
            await db.DeleteAsync<PostEmail>(x => x.PostId == postId, token: token);
            appConfig.ResetInitialPostId(db);
        }
    }
}
