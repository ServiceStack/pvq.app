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
public class PostSubscriptionsCommand(IDbConnection db) : AsyncCommand<PostSubscriptions>
{
    protected override async Task RunAsync(PostSubscriptions request, CancellationToken token)
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
            await db.InsertAllAsync(subs, token: token);
        }
        if (request.Unsubscriptions is { Count: > 0 })
        {
            await db.DeleteAsync<WatchPost>(
                x => x.UserName == request.UserName && request.Unsubscriptions.Contains(x.PostId), token: token);
        }
    }
}
