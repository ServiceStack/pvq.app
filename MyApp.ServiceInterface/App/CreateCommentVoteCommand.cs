using System.Data;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Database)]
public class CreateCommentVoteCommand(IDbConnection db, QuestionsProvider questions) : IAsyncCommand<Vote>
{
    public async Task ExecuteAsync(Vote vote)
    {
        if (string.IsNullOrEmpty(vote.RefId))
            throw new ArgumentNullException(nameof(vote.RefId));
        if (string.IsNullOrEmpty(vote.UserName))
            throw new ArgumentNullException(nameof(vote.UserName));

        var rowsDeleted = await db.DeleteAsync<Vote>(new { vote.RefId, vote.UserName });

        var meta = await questions.GetMetaAsync(vote.PostId);
        var created = vote.RefId.LastRightPart('-').ToLong();
        var commentTargetRefId = vote.RefId.LastLeftPart('-');
        if (meta.Comments.TryGetValue(commentTargetRefId, out var comments))
        {
            var comment = comments.Find(x => x.Created == created);
            if (comment != null)
            {
                if (comment.CreatedBy == vote.UserName)
                    throw new ArgumentException("Can't vote on your own comment", nameof(vote.RefId));

                vote.RefUserName = comment.CreatedBy;
                await db.InsertAsync(vote);

                comment.UpVotes = (int) await db.CountAsync<Vote>(x => x.RefId == vote.RefId && x.Score > 0);
                await questions.SaveMetaAsync(vote.PostId, meta);
            }
        }
    }
}
