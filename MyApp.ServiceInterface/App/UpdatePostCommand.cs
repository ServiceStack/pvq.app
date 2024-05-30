using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Questions)]
public class UpdatePostCommand(IDbConnection db) : IAsyncCommand<Post>
{
    public async Task ExecuteAsync(Post question)
    {
        await db.UpdateOnlyAsync(() => new Post {
            Title = question.Title,
            Tags = question.Tags,
            Slug = question.Slug,
            Summary = question.Summary,
            ModifiedBy = question.ModifiedBy,
            LastActivityDate = question.LastActivityDate,
            LastEditDate = question.LastEditDate,
        }, x => x.Id == question.Id);
    }
}