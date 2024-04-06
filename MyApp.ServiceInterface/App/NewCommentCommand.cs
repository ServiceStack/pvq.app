using System.Data;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class NewCommentCommand(AppConfig appConfig, IDbConnection db) : IExecuteCommandAsync<NewComment>
{
    public async Task ExecuteAsync(NewComment request)
    {
        var refId = request.RefId;
        var postId = refId.LeftPart('-').ToInt();
        var post = await db.SingleByIdAsync<Post>(postId);
        if (post != null)
        {
            var isAnswer = refId.IndexOf('-') > 0;
            var createdBy = isAnswer
                ? (await db.SingleByIdAsync<StatTotals>(refId))?.CreatedBy
                : post.CreatedBy;

            var comment = request.Comment;
            var commentRefId = $"{refId}-{comment.Created}";
            var cleanBody = comment.Body.StripHtml().Trim();
            var createdDate = DateTimeOffset.FromUnixTimeMilliseconds(comment.Created).DateTime;

            if (createdBy != null && createdBy != comment.CreatedBy)
            {
                await db.InsertAsync(new Notification
                {
                    UserName = createdBy,
                    Type = NotificationType.NewComment,
                    RefId = commentRefId,
                    PostId = postId,
                    CreatedDate = createdDate,
                    Summary = cleanBody.SubstringWithEllipsis(0, 100),
                    RefUserName = comment.CreatedBy,
                });
                appConfig.IncrUnreadNotificationsFor(createdBy);
            }

            var userNameMentions = cleanBody.FindUserNameMentions()
                .Where(x => x != createdBy && x != comment.CreatedBy).ToList();
            if (userNameMentions.Count > 0)
            {
                var existingUsers = await db.SelectAsync(db.From<ApplicationUser>()
                    .Where(x => userNameMentions.Contains(x.UserName!)));

                foreach (var existingUser in existingUsers)
                {
                    var firstMentionPos = cleanBody.IndexOf(existingUser.UserName!, StringComparison.Ordinal);
                    if (firstMentionPos < 0) continue;

                    var startPos = Math.Max(0, firstMentionPos - 50);
                    await db.InsertAsync(new Notification
                    {
                        UserName = existingUser.UserName!,
                        Type = NotificationType.CommentMention,
                        RefId = commentRefId,
                        PostId = postId,
                        CreatedDate = createdDate,
                        Summary = cleanBody.SubstringWithEllipsis(startPos, 100),
                        RefUserName = comment.CreatedBy,
                    });
                    appConfig.IncrUnreadNotificationsFor(existingUser.UserName!);
                }
            }
        }
    }
}