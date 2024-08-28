using System.Data;
using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Answers)]
[Worker(Databases.App)]
public class CreateAnswerCommand(ILogger<CreateAnswerCommand> logger, 
    IBackgroundJobs jobs, AppConfig appConfig, IDbConnection db) 
    : SyncCommand<Post>
{
    protected override void Run(Post answer)
    {
        if (answer.ParentId == null)
            throw new ArgumentNullException(nameof(answer.ParentId));
        if (answer.CreatedBy == null)
            throw new ArgumentNullException(nameof(answer.CreatedBy));

        var log = Request.CreateJobLogger(jobs, logger);
        var postId = answer.ParentId!.Value;
        var refId = $"{postId}-{answer.CreatedBy}";
        if (!db.Exists(db.From<StatTotals>().Where(x => x.Id == refId)))
        {
            log.LogInformation("Adding StatTotals {Id} for Post {PostId}", refId, postId);
            db.Insert(new StatTotals
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

        var post = db.SingleById<Post>(postId);
        if (post?.CreatedBy != null)
        {
            // Notify Post Author of new Answer
            if (post.CreatedBy != answer.CreatedBy && appConfig.IsHuman(post.CreatedBy))
            {
                log.LogInformation("Notify Post Author {User} of new Answer {Id} for Post {PostId}", post.CreatedBy, refId, postId);
                db.Insert(new Notification
                {
                    UserName = post.CreatedBy,
                    Type = NotificationType.NewAnswer,
                    RefId = refId,
                    PostId = postId,
                    CreatedDate = answer.CreationDate,
                    Summary = answer.Summary,
                    RefUserName = answer.CreatedBy,
                });
                appConfig.IncrUnreadNotificationsFor(post.CreatedBy);
            }

            // Notify any User Mentions in Answer
            if (!string.IsNullOrEmpty(answer.Body))
            {
                var cleanBody = answer.Body.StripHtml().Trim();
                var userNameMentions = cleanBody.FindUserNameMentions()
                    .Where(x => x != post.CreatedBy && x != answer.CreatedBy && appConfig.IsHuman(x)).ToList();
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
                            log.LogInformation("Notify Post User Mention {User} for Answer {Id} in Post {PostId}", existingUser.UserName, refId, postId);
                            db.Insert(new Notification
                            {
                                UserName = existingUser.UserName!,
                                Type = NotificationType.AnswerMention,
                                RefId = $"{postId}",
                                PostId = postId,
                                CreatedDate = answer.CreationDate,
                                Summary = cleanBody.GenerateNotificationSummary(startPos),
                                RefUserName = answer.CreatedBy,
                            });
                            appConfig.IncrUnreadNotificationsFor(existingUser.UserName!);
                        }
                    }
                }
            }
        }

        if (appConfig.IsHuman(answer.CreatedBy))
        {
            db.Insert(new Achievement
            {
                UserName = answer.CreatedBy,
                Type = AchievementType.NewAnswer,
                RefId = refId,
                PostId = postId,
                Score = 1,
                CreatedDate = DateTime.UtcNow,
            });
            appConfig.IncrUnreadAchievementsFor(answer.CreatedBy);
        }
    }
}
