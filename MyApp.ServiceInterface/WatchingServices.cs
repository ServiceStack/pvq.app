using MyApp.Data;
using MyApp.ServiceInterface.App;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class WatchingServices(IBackgroundJobs jobs) : Service
{
    public object Any(WatchContent request)
    {
        var userName = Request.GetClaimsPrincipal().GetRequiredUserName();
        if (request.PostId == null && request.Tag == null)
            throw new ArgumentException("PostId or Tag is required", nameof(request.PostId));

        if (request.PostId != null)
        {
            jobs.RunCommand<PostSubscriptionsCommand>(new PostSubscriptions {
                UserName = userName,
                Subscriptions = [request.PostId.Value],
            });
        }
        if (request.Tag != null)
        {
            jobs.RunCommand<TagSubscriptionsCommand>(new TagSubscriptions {
                UserName = userName,
                Subscriptions = [request.Tag],
            });
        }
        return new EmptyResponse();
    }

    public object Any(UnwatchContent request)
    {
        var userName = Request.GetClaimsPrincipal().GetRequiredUserName();
        if (request.PostId == null && request.Tag == null)
            throw new ArgumentException("PostId or Tag is required", nameof(request.PostId));

        if (request.PostId != null)
        {
            jobs.RunCommand<PostSubscriptionsCommand>(new PostSubscriptions {
                UserName = userName,
                Unsubscriptions = [request.PostId.Value],
            });
        }
        if (request.Tag != null)
        {
            jobs.RunCommand<TagSubscriptionsCommand>(new TagSubscriptions {
                UserName = userName,
                Unsubscriptions = [request.Tag],
            });
        }
        return new EmptyResponse();
    }

    public async Task<object> Any(WatchStatus request)
    {
        var userName = Request.GetClaimsPrincipal().GetRequiredUserName();
        if (request.PostId == null && request.Tag == null)
            throw new ArgumentException("PostId or Tag is required", nameof(request.PostId));

        var watching = request.PostId != null
            ? Db.IsWatchingPost(userName, request.PostId.Value)
            : Db.IsWatchingTag(userName, request.Tag!);
        
        return new BoolResponse { Result = watching };
    }
    
    public object Any(WatchTags request)
    {
        var userName = Request.GetClaimsPrincipal().GetRequiredUserName();
        jobs.RunCommand<TagSubscriptionsCommand>(new TagSubscriptions {
            UserName = userName,
            Subscriptions = request.Subscribe,
            Unsubscriptions = request.Unsubscribe,
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
