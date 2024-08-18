using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Worker(Databases.App)]
[Tag(Tags.Questions)]
public class UpdatePostCommand(IDbConnection db) : AsyncCommand<Post>
{
    protected override async Task RunAsync(Post question, CancellationToken token)
    {
        await db.UpdateOnlyAsync(() => new Post {
            Title = question.Title,
            Tags = question.Tags,
            Slug = question.Slug,
            Summary = question.Summary,
            ModifiedBy = question.ModifiedBy,
            LastActivityDate = question.LastActivityDate,
            LastEditDate = question.LastEditDate,
        }, x => x.Id == question.Id, token: token);
    }
}
