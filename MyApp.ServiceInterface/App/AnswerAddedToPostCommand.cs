using System.Data;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class AnswerAddedToPostCommand(IDbConnection db) : IExecuteCommandAsync<AnswerAddedToPost>
{
    public async Task ExecuteAsync(AnswerAddedToPost request)
    {
        await db.UpdateAddAsync(() => new Post {
            AnswerCount = 1,
        }, x => x.Id == request.Id);
    }
}