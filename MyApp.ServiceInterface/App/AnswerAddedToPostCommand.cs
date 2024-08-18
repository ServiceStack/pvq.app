using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Answers)]
[Worker(Databases.App)]
public class AnswerAddedToPostCommand(IDbConnection db) : AsyncCommand<AnswerAddedToPost>
{
    protected override async Task RunAsync(AnswerAddedToPost request, CancellationToken token)
    {
        await db.UpdateAddAsync(() => new Post {
            AnswerCount = 1,
        }, x => x.Id == request.Id, token:token);
    }
}