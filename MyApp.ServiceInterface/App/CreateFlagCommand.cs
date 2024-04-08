using System.Data;
using MyApp.ServiceModel;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class CreateFlagCommand(IDbConnection db) : IAsyncCommand<Flag>
{
    public async Task ExecuteAsync(Flag request)
    {
        await db.InsertAsync(request);
    }
}
