using System.Data;
using ServiceStack;
using MyApp.Data;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Notifications)]
public class UpdateReputationsCommand(AppConfig appConfig, IDbConnection db) : IAsyncCommand<UpdateReputations>
{
    public async Task ExecuteAsync(UpdateReputations request)
    {
        // TODO improve
        appConfig.UpdateUsersReputation(db);
        appConfig.ResetUsersReputation(db);
    }
}