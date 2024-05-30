using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Answers)]
public class AnswerAddedToPostCommand(IDbConnection db) : IAsyncCommand<AnswerAddedToPost>
{
    public async Task ExecuteAsync(AnswerAddedToPost request)
    {
        await db.UpdateAddAsync(() => new Post {
            AnswerCount = 1,
        }, x => x.Id == request.Id);
    }
}