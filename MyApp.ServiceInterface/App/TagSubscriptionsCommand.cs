using System.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Notifications)]
public class TagSubscriptionsCommand(IDbConnection db) : IAsyncCommand<TagSubscriptions>
{
    public async Task ExecuteAsync(TagSubscriptions request)
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
            await db.InsertAllAsync(subs);
        }
        if (request.Unsubscriptions is { Count: > 0 })
        {
            await db.DeleteAsync<WatchTag>(x => x.UserName == request.UserName && request.Unsubscriptions.Contains(x.Tag));
        }
    }
}
