using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Database)]
public class CreateFlagCommand(IDbConnection db) : IAsyncCommand<Flag>
{
    public async Task ExecuteAsync(Flag request)
    {
        await db.InsertAsync(request);
    }
}
