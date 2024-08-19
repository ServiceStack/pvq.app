using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Worker(Databases.App)]
[Tag(Tags.Answers)]
public class DeleteAnswersCommand(IDbConnection db) : SyncCommand<DeleteAnswers>
{
    protected override void Run(DeleteAnswers request)
    {
        foreach (var refId in request.Ids)
        {
            db.Delete<Vote>(x => x.RefId == refId);
            db.DeleteById<Post>(refId);
            db.Delete<StatTotals>(x => x.Id == refId);
            db.Delete<Notification>(x => x.RefId == refId);
            db.Delete<PostEmail>(x => x.RefId == refId);
        }
    }
}
