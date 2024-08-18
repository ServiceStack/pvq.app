using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Worker(Databases.App)]
[Tag(Tags.Answers)]
public class DeleteAnswersCommand(IDbConnection db) : AsyncCommand<DeleteAnswers>
{
    protected override async Task RunAsync(DeleteAnswers request, CancellationToken token)
    {
        foreach (var refId in request.Ids)
        {
            await db.DeleteAsync<Vote>(x => x.RefId == refId, token: token);
            await db.DeleteByIdAsync<Post>(refId, token: token);
            await db.DeleteAsync<StatTotals>(x => x.Id == refId, token: token);
            await db.DeleteAsync<Notification>(x => x.RefId == refId, token: token);
            await db.DeleteAsync<PostEmail>(x => x.RefId == refId, token: token);
        }
    }
}
