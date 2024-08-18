using System.Data;
using ServiceStack;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Worker(Databases.App)]
[Tag(Tags.Notifications)]
public class UpdateReputationsCommand(AppConfig appConfig, IDbConnection db) : SyncCommand
{
    protected override void Run()
    {
        // TODO improve
        appConfig.UpdateUsersReputation(db);
        appConfig.ResetUsersReputation(db);
    }
}
