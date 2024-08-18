using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

[Worker(Databases.Analytics)]
public class AnalyticsTasksCommand(R2VirtualFiles r2, QuestionsProvider questions) : AsyncCommand<AnalyticsTasks>
{
    protected override async Task RunAsync(AnalyticsTasks request, CancellationToken token)
    {
        if (request.CreatePostStat == null && request.CreateSearchStat == null && request.DeletePost == null)
            return;

        using var analyticsDb = HostContext.AppHost.GetDbConnection(Databases.Analytics);
        
        if (request.CreatePostStat != null)// && !Stats.IsAdminOrModerator(request.RecordPostView.UserName))
        {
            await analyticsDb.InsertAsync(request.CreatePostStat, token: token);
        }

        if (request.CreateSearchStat != null)// && !Stats.IsAdminOrModerator(request.RecordSearchView.UserName))
        {
            await analyticsDb.InsertAsync(request.CreateSearchStat, token: token);
        }

        if (request.DeletePost != null)
        {
            await analyticsDb.DeleteAsync<PostStat>(x => x.PostId == request.DeletePost, token: token);
        }
    }
}
