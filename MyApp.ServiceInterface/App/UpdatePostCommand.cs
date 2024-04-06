using System.Data;
using MyApp.ServiceModel;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class UpdatePostCommand(IDbConnection db) : IExecuteCommandAsync<Post>
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