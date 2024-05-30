using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Answers)]
public class DeleteAnswersCommand(IDbConnection db) : IAsyncCommand<DeleteAnswers>
{
    public async Task ExecuteAsync(DeleteAnswers request)
    {
        foreach (var refId in request.Ids)
        {
            await db.DeleteAsync<Vote>(x => x.RefId == refId);
            await db.DeleteByIdAsync<Post>(refId);
            await db.DeleteAsync<StatTotals>(x => x.Id == refId);
            await db.DeleteAsync<Notification>(x => x.RefId == refId);
            await db.DeleteAsync<PostEmail>(x => x.RefId == refId);
        }
    }
}
