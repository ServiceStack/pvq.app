using System.Data;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class CreateAnswerCommand(AppConfig appConfig, IDbConnection db) : IExecuteCommandAsync<Post>
{
    public async Task ExecuteAsync(Post answer)
    {
        if (answer.ParentId == null)
            throw new ArgumentNullException(nameof(answer.ParentId));
        if (answer.CreatedBy == null)
            throw new ArgumentNullException(nameof(answer.CreatedBy));
        
        var postId = answer.ParentId!.Value;
        var refId = $"{postId}-{answer.CreatedBy}";
        if (!await db.ExistsAsync(db.From<StatTotals>().Where(x => x.Id == refId)))
        {
            await db.InsertAsync(new StatTotals
            {
                Id = refId,
                PostId = postId,
                ViewCount = 0,
                FavoriteCount = 0,
                UpVotes = 0,
                DownVotes = 0,
                StartingUpVotes = 0,
                CreatedBy = answer.CreatedBy,
            });
        }

        var post = await db.SingleByIdAsync<Post>(postId);
        if (post?.CreatedBy != null)
        {
            if (post.CreatedBy != answer.CreatedBy)
            {
                await db.InsertAsync(new Notification
                {
                    UserName = post.CreatedBy,
                    Type = NotificationType.NewAnswer,
                    RefId = refId,
                    PostId = postId,
                    CreatedDate = answer.CreationDate,
                    Summary = answer.Summary.SubstringWithEllipsis(0, 100),
                    RefUserName = answer.CreatedBy,
                });
                appConfig.IncrUnreadNotificationsFor(post.CreatedBy);
            }

            if (!string.IsNullOrEmpty(answer.Body))
            {
                var cleanBody = answer.Body.StripHtml().Trim();
                var userNameMentions = cleanBody.FindUserNameMentions()
                    .Where(x => x != post.CreatedBy && x != answer.CreatedBy).ToList();
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
                            Type = NotificationType.AnswerMention,
                            RefId = $"{postId}",
                            PostId = postId,
                            CreatedDate = answer.CreationDate,
                            Summary = cleanBody.SubstringWithEllipsis(startPos, 100),
                            RefUserName = answer.CreatedBy,
                        });
                        appConfig.IncrUnreadNotificationsFor(existingUser.UserName!);
                    }
                }
            }
        }
    }
}