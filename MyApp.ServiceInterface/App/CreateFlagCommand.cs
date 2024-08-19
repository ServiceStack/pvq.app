using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Database)]
[Worker(Databases.App)]
public class CreateFlagCommand(IDbConnection db) : SyncCommand<Flag>
{
    protected override void Run(Flag request) => db.Insert(request);
}
