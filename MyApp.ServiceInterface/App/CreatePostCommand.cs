using System.Data;
using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

[Worker(Databases.App)]
[Tag(Tags.Questions)]
public class CreatePostCommand(ILogger<CreatePostCommand> logger, IBackgroundJobs jobs, AppConfig appConfig, IDbConnection db) : AsyncCommand<Post>
{
    protected override async Task RunAsync(Post post, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        var body = post.Body;
        post.Body = null;
        
        if (post.Id > 0)
        {
            db.Insert(post);
        }
        else
        {
            post.Id = (int)db.Insert(post, selectIdentity: true);
        }
        
        var createdBy = post.CreatedBy;
        if (createdBy != null && post.PostTypeId == 1)
        {
            appConfig.ResetUserQuestions(db, createdBy);
        }

        try
        {
            db.Insert(new StatTotals
            {
                Id = $"{post.Id}",
                PostId = post.Id,
                UpVotes = 0,
                DownVotes = 0,
                StartingUpVotes = 0,
                CreatedBy = post.CreatedBy,
            });
        }
        catch (Exception e)
        {
            log.LogWarning("Couldn't insert StatTotals for Post {PostId}: '{Message}', updating instead...", post.Id,
                e.Message);
            db.UpdateOnly(() => new StatTotals
            {
                PostId = post.Id,
                CreatedBy = post.CreatedBy,
            }, x => x.Id == $"{post.Id}");
        }

        if (!string.IsNullOrEmpty(body))
        {
            var cleanBody = body.StripHtml().Trim();
            var userNameMentions = cleanBody.FindUserNameMentions()
                .Where(x => x != createdBy && appConfig.IsHuman(x)).ToList();
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
                            Type = NotificationType.QuestionMention,
                            RefId = $"{post.Id}",
                            PostId = post.Id,
                            CreatedDate = post.CreationDate,
                            Summary = cleanBody.GenerateNotificationSummary(startPos),
                            RefUserName = createdBy,
                        });
                        appConfig.IncrUnreadNotificationsFor(existingUser.UserName!);
                    }
                }
            }
        }

        if (appConfig.IsHuman(post.CreatedBy))
        {
            db.Insert(new Achievement
            {
                UserName = post.CreatedBy!,
                Type = AchievementType.NewQuestion,
                RefId = $"{post.Id}",
                PostId = post.Id,
                Score = 1,
                CreatedDate = DateTime.UtcNow,
            });
            appConfig.IncrUnreadAchievementsFor(post.CreatedBy!);

            // Setup auto-watch for new questions (Sending Emails for new Answers)
            db.Insert(new WatchPost
            {
                UserName = post.CreatedBy!,
                PostId = post.Id,
                CreatedDate = post.CreationDate,
                // Email new answers 1hr after asking question
                AfterDate = DateTime.UtcNow.Add(TimeSpan.FromHours(1)),
            });
        }
    }
}
