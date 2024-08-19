using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Worker(Databases.App)]
[Tag(Tags.Questions)]
public class UpdatePostCommand(IDbConnection db) : SyncCommand<Post>
{
    protected override void Run(Post question)
    {
        db.UpdateOnly(() => new Post
        {
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
