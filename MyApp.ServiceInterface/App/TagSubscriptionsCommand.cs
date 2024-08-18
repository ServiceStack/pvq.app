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
public class TagSubscriptionsCommand(IDbConnection db) : AsyncCommand<TagSubscriptions>
{
    protected override async Task RunAsync(TagSubscriptions request, CancellationToken token)
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
            await db.InsertAllAsync(subs, token: token);
        }
        if (request.Unsubscriptions is { Count: > 0 })
        {
            await db.DeleteAsync<WatchTag>(
                x => x.UserName == request.UserName && request.Unsubscriptions.Contains(x.Tag), token: token);
        }
    }
}
