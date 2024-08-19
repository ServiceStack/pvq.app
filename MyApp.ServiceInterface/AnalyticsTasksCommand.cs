using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

[Worker(Databases.Analytics)]
public class AnalyticsTasksCommand(IDbConnectionFactory dbFactory) : SyncCommand<AnalyticsTasks>
{
    protected override void Run(AnalyticsTasks request)
    {
        if (request.CreatePostStat == null && request.CreateSearchStat == null && request.DeletePost == null)
            return;

        using var analyticsDb = dbFactory.Open(Databases.Analytics);
        
        if (request.CreatePostStat != null)// && !Stats.IsAdminOrModerator(request.RecordPostView.UserName))
        {
            analyticsDb.Insert(request.CreatePostStat);
        }

        if (request.CreateSearchStat != null)// && !Stats.IsAdminOrModerator(request.RecordSearchView.UserName))
        {
            analyticsDb.Insert(request.CreateSearchStat);
        }

        if (request.DeletePost != null)
        {
            analyticsDb.Delete<PostStat>(x => x.PostId == request.DeletePost);
        }
    }
}
