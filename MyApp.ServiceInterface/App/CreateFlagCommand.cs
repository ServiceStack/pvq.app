using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Database)]
[Worker(Databases.App)]
public class CreateFlagCommand(IDbConnection db) : AsyncCommand<Flag>
{
    protected override async Task RunAsync(Flag request, CancellationToken token)
    {
        await db.InsertAsync(request, token: token);
    }
}
