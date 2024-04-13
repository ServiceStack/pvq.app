using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class WatchingServices : Service
{
    public object Any(WatchContent request)
    {
        var userName = Request.GetClaimsPrincipal().GetRequiredUserName();
        if (request.PostId == null && request.Tag == null)
            throw new ArgumentException("PostId or Tag is required", nameof(request.PostId));
        
        MessageProducer.Publish(new DbWrites
        {
            PostSubscriptions = request.PostId == null ? null : new()
            {
                UserName = userName,
                Subscriptions = [request.PostId.Value],
            },
            TagSubscriptions = request.Tag == null ? null : new()
            {
                UserName = userName,
                Subscriptions = [request.Tag],
            },
        });
        return new EmptyResponse();
    }

    public object Any(UnwatchContent request)
    {
        var userName = Request.GetClaimsPrincipal().GetRequiredUserName();
        if (request.PostId == null && request.Tag == null)
            throw new ArgumentException("PostId or Tag is required", nameof(request.PostId));

        MessageProducer.Publish(new DbWrites
        {
            PostSubscriptions = request.PostId == null ? null : new()
            {
                UserName = userName,
                Unsubscriptions = [request.PostId.Value],
            },
            TagSubscriptions = request.Tag == null ? null : new()
            {
                UserName = userName,
                Unsubscriptions = [request.Tag],
            },
        });
        return new EmptyResponse();
    }

    public async Task<object> Any(WatchStatus request)
    {
        var userName = Request.GetClaimsPrincipal().GetRequiredUserName();
        if (request.PostId == null && request.Tag == null)
            throw new ArgumentException("PostId or Tag is required", nameof(request.PostId));

        var watching = request.PostId != null
            ? await Db.IsWatchingPostAsync(userName, request.PostId.Value)
            : await Db.IsWatchingTagAsync(userName, request.Tag!);
        
        return new BoolResponse { Result = watching };
    }
    
    public object Any(WatchTags request)
    {
        var userName = Request.GetClaimsPrincipal().GetRequiredUserName();
        MessageProducer.Publish(new DbWrites
        {
            TagSubscriptions = new()
            {
                UserName = userName,
                Subscriptions = request.Subscribe,
                Unsubscriptions = request.Unsubscribe,
            }
        });
        return new EmptyResponse();
    }

    public async Task<object> Any(GetWatchedTags request)
    {
        var userName = Request.GetClaimsPrincipal().GetRequiredUserName();
        var tags = await Db.ColumnAsync<string>(Db.From<WatchTag>().Where(x => x.UserName == userName).Select(x => x.Tag));
        return new GetWatchedTagsResponse
        {
            Results = tags,
        };
    }
}
