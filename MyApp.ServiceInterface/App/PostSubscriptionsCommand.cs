using System.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class PostSubscriptions
{
    public required string UserName { get; set; }
    public List<int>? Subscriptions { get; set; }
    public List<int>? Unsubscriptions { get; set; }
}

[Worker(Databases.App)]
[Tag(Tags.Notifications)]
public class PostSubscriptionsCommand(IDbConnection db) : SyncCommand<PostSubscriptions>
{
    protected override void Run(PostSubscriptions request)
    {
        var now = DateTime.UtcNow;
        if (request.Subscriptions is { Count: > 0 })
        {
            var subs = request.Subscriptions.Map(x => new WatchPost
            {
                UserName = request.UserName,
                PostId = x,
                CreatedDate = now,
                AfterDate = now,
            });
            db.InsertAll(subs);
        }
        if (request.Unsubscriptions is { Count: > 0 })
        {
            db.Delete<WatchPost>(
                x => x.UserName == request.UserName && request.Unsubscriptions.Contains(x.PostId));
        }
    }
}
