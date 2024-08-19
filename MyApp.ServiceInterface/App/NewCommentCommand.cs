using System.Data;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class NewComment
{
    // Post or AnswerId
    public string RefId { get; set; }
    public Comment Comment { get; set; }
    public DateTime LastUpdated { get; set; }
}

[Tag(Tags.Database)]
[Worker(Databases.App)]
public class NewCommentCommand(AppConfig appConfig, IDbConnection db) : SyncCommand<NewComment>
{
    protected override void Run(NewComment request)
    {
        var refId = request.RefId;
        var postId = refId.LeftPart('-').ToInt();
        var post = db.SingleById<Post>(postId);
        if (post != null)
        {
            var isAnswer = refId.IndexOf('-') > 0;
            var createdBy = isAnswer
                ? db.SingleById<StatTotals>(refId)?.CreatedBy
                : post.CreatedBy;

            var comment = request.Comment;
            var commentRefId = $"{refId}-{comment.Created}";
            var cleanBody = comment.Body.StripHtml().Trim();
            var createdDate = DateTimeOffset.FromUnixTimeMilliseconds(comment.Created).DateTime;

            if (createdBy != null && createdBy != comment.CreatedBy && appConfig.IsHuman(createdBy))
            {
                db.Insert(new Notification
                {
                    UserName = createdBy,
                    Type = NotificationType.NewComment,
                    RefId = commentRefId,
                    PostId = postId,
                    CreatedDate = createdDate,
                    Summary = cleanBody.GenerateNotificationSummary(),
                    RefUserName = comment.CreatedBy,
                });
                appConfig.IncrUnreadNotificationsFor(createdBy);
            }

            var lastUpdated = request.LastUpdated;
            appConfig.SetLastUpdated(request.RefId, lastUpdated);
            db.UpdateOnly(() => new StatTotals
            {
                LastUpdated = lastUpdated,
            }, where: x => x.Id == request.RefId);

            var userNameMentions = cleanBody.FindUserNameMentions()
                .Where(x => x != createdBy && x != comment.CreatedBy && appConfig.IsHuman(x))
                .ToList();
            if (userNameMentions.Count > 0)
            {
                var existingUsers = db.Select(db.From<ApplicationUser>()
                    .Where(x => userNameMentions.Contains(x.UserName!)));

                foreach (var existingUser in existingUsers)
                {
                    var firstMentionPos = cleanBody.IndexOf(existingUser.UserName!, StringComparison.Ordinal);
                    if (firstMentionPos < 0) continue;

                    var startPos = Math.Max(0, firstMentionPos - 50);
                    if (appConfig.IsHuman(existingUser.UserName))
                    {
                        db.Insert(new Notification
                        {
                            UserName = existingUser.UserName!,
                            Type = NotificationType.CommentMention,
                            RefId = commentRefId,
                            PostId = postId,
                            CreatedDate = createdDate,
                            Summary = cleanBody.GenerateNotificationSummary(startPos),
                            RefUserName = comment.CreatedBy,
                        });
                        appConfig.IncrUnreadNotificationsFor(existingUser.UserName!);
                    }
                }
            }
        }
    }
}
