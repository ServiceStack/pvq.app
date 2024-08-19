using System.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class TagSubscriptions
{
    public required string UserName { get; set; }
    public List<string>? Subscriptions { get; set; }
    public List<string>? Unsubscriptions { get; set; }
}

[Worker(Databases.App)]
[Tag(Tags.Notifications)]
public class TagSubscriptionsCommand(IDbConnection db) : SyncCommand<TagSubscriptions>
{
    protected override void Run(TagSubscriptions request)
    {
        var now = DateTime.UtcNow;
        if (request.Subscriptions is { Count: > 0 })
        {
            var subs = request.Subscriptions.Map(x => new WatchTag
            {
                UserName = request.UserName,
                Tag = x,
                CreatedDate = now,
            });
            db.InsertAll(subs);
        }
        if (request.Unsubscriptions is { Count: > 0 })
        {
            db.Delete<WatchTag>(
                x => x.UserName == request.UserName && request.Unsubscriptions.Contains(x.Tag));
        }
    }
}
