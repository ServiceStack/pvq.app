using System.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class PostSubscriptionsCommand(IDbConnection db) : IAsyncCommand<PostSubscriptions>
{
    public async Task ExecuteAsync(PostSubscriptions request)
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
            await db.InsertAllAsync(subs);
        }
        if (request.Unsubscriptions is { Count: > 0 })
        {
            await db.DeleteAsync<WatchPost>(x => x.UserName == request.UserName && request.Unsubscriptions.Contains(x.PostId));
        }
    }
}
