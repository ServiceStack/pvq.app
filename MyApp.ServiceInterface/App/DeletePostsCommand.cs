using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Worker(Databases.App)]
[Tag(Tags.Database)]
public class DeletePostsCommand(AppConfig appConfig, IDbConnection db) 
    : SyncCommand<DeletePosts>
{
    protected override void Run(DeletePosts request)
    {
        foreach (var postId in request.Ids)
        {
            db.Delete<Vote>(x => x.PostId == postId);
            db.DeleteById<Post>(postId);
            db.Delete<StatTotals>(x => x.PostId == postId);
            db.Delete<Notification>(x => x.PostId == postId);
            db.Delete<WatchPost>(x => x.PostId == postId);
            db.Delete<PostEmail>(x => x.PostId == postId);
            appConfig.ResetInitialPostId(db);
        }
    }
}
