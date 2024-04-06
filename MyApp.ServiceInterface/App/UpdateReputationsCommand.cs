using System.Data;
using MyApp.Data;

namespace MyApp.ServiceInterface.App;

public class UpdateReputationsCommand(AppConfig appConfig, IDbConnection db) : IExecuteCommandAsync<UpdateReputations>
{
    public async Task ExecuteAsync(UpdateReputations request)
    {
        // TODO improve
        appConfig.UpdateUsersReputation(db);
        appConfig.ResetUsersReputation(db);
    }
}