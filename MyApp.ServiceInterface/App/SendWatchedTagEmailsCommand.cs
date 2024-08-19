using CreatorKit.ServiceInterface;
using CreatorKit.ServiceModel;
using CreatorKit.ServiceModel.Types;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;
using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceInterface.CreatorKit;
using ServiceStack.Data;
using ServiceStack.Jobs;
using ServiceStack.Script;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Database)]
[Worker(Databases.App)]
public class SendWatchedTagEmailsCommand(ILogger<SendWatchedTagEmailsCommand> logger, 
    IBackgroundJobs jobs, IDbConnectionFactory dbFactory, EmailRenderer renderer) 
    : AsyncCommand
{
    protected override async Task RunAsync(CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        //var job = Request.GetBackgroundJob();
        var yesterday = DateTime.UtcNow.AddDays(-1).Date;
        var day = yesterday.ToString("yyyy-MM-dd");
        using var db = await dbFactory.OpenDbConnectionAsync(token:token);
        if (await db.ExistsAsync(db.From<WatchPostMail>().Where(x => x.Date == day), token:token))
            return;

        var newPosts = await db.SelectAsync(db.From<Post>().Where(x =>
            x.CreationDate >= yesterday && x.CreationDate < yesterday.AddDays(1)), token:token);
        if (newPosts.Count == 0)
        {
            log.LogInformation("No new posts found for {Date}", day);
            return;
        }

        var tagGroups = new Dictionary<string, List<Post>>();
        foreach (var post in newPosts)
        {
            foreach (var tag in post.Tags)
            {
                if (!tagGroups.TryGetValue(tag, out var posts))
                    tagGroups[tag] = posts = [];
                posts.Add(post);
            }
        }

        var uniqueTags = tagGroups.Keys.ToSet();
        var watchTags = await db.SelectAsync(db.From<WatchTag>()
            .Where(x => uniqueTags.Contains(x.Tag)), token: token);
        if (watchTags.Count == 0)
        {
            log.LogInformation("No Tag Watchers found for {Date}", day);
            return;
        }
        
        var uniqueUserNames = watchTags.Select(x => x.UserName).ToSet();
        var users = await db.SelectAsync<ApplicationUser>(
            x => uniqueUserNames.Contains(x.UserName!), token: token);

        using var dbCreatorKit = await dbFactory.OpenDbConnectionAsync(Databases.CreatorKit, token:token);

        var mailRuns = 0;
        var orderedTags = uniqueTags.OrderBy(x => x).ToList();
        foreach (var tag in orderedTags)
        {
            if (!tagGroups.TryGetValue(tag, out var posts))
                continue;

            var tagWatchers = watchTags.Where(x => x.Tag == tag).ToList();
            if (tagWatchers.Count == 0)
                continue;

            var postIds = posts.ConvertAll(x => x.Id);

            var userNames = tagWatchers.Map(x => x.UserName);
            var watchPostMail = new WatchPostMail
            {
                Date = day,
                Tag = tag,
                UserNames = userNames,
                PostIds = postIds,
                CreatedDate = DateTime.UtcNow,
            };
            watchPostMail.Id = (int)await db.InsertAsync(watchPostMail, selectIdentity: true, token:token);
            log.LogInformation("Created {Day} WatchPostMail {Id} for {Tag} with posts:{PostIds} for users:{UserNames}",
                day, watchPostMail.Id, tag, postIds.Join(","), userNames.Join(","));
            
            var layout = "tags";
            var template = "tagged-questions";
            var context = renderer.CreateMailContext(layout: layout, page: template);
            var monthDay = yesterday.ToString("MMMM dd");
            var args = new Dictionary<string, object>
            {
                ["tag"] = tag,
                ["date"] = monthDay,
                ["posts"] = posts,
            };
            var html = await new PageResult(context.GetPage("content"))
            {
                Layout = "layout",
                Args = args,
            }.RenderToStringAsync();

            args.Remove("model");
            
            var externalRef = $"{nameof(WatchPostMail)}:{watchPostMail.Id}";
            var mailRun = new MailRun
            {
                MailingList = MailingList.WatchedTags,
                CreatedDate = DateTime.UtcNow,
                Layout = layout,
                Generator = nameof(RenderTagQuestionsEmail),
                Template = template,
                GeneratorArgs = args,
                ExternalRef = externalRef,
            };
            mailRun.Id = (int)await dbCreatorKit.InsertAsync(mailRun, selectIdentity: true, token:token);
            mailRuns++;

            await db.UpdateOnlyAsync(() => new WatchPostMail
            {
                MailRunId = mailRun.Id,
            }, where: x => x.Id == watchPostMail.Id, token:token);

            var emails = 0;
            foreach (var tagWatcher in tagWatchers)
            {
                var user = users.Find(x => x.UserName == tagWatcher.UserName);
                if (user == null)
                {
                    log.LogInformation("User {UserName} not found for WatchTag {Tag}",
                        tagWatcher.UserName, tagWatcher.Tag);
                    continue;
                }

                var message = new EmailMessage
                {
                    To = [new() { Email = user.Email!, Name = user.UserName! }],
                    Subject = $"New {tag} questions for {monthDay} - pvq.app",
                    BodyHtml = html,
                };

                var contact = await dbCreatorKit.GetOrCreateContact(user);

                var mailMessage = new MailMessageRun
                {
                    MailRunId = mailRun.Id,
                    ContactId = contact.Id,
                    Contact = contact,
                    Renderer = nameof(RenderTagQuestionsEmail),
                    RendererArgs = args,
                    Message = message,
                    CreatedDate = DateTime.UtcNow,
                    ExternalRef = externalRef,
                };
                mailMessage.Id = (int)await dbCreatorKit.InsertAsync(mailMessage, selectIdentity: true, token:token);
                emails++;
            }

            var generatedDate = DateTime.UtcNow;
            await db.UpdateOnlyAsync(() => new WatchPostMail
            {
                GeneratedDate = generatedDate,
            }, where: x => x.Id == watchPostMail.Id, token: token);
            await dbCreatorKit.UpdateOnlyAsync(() => new MailRun
            {
                EmailsCount = emails,
                GeneratedDate = generatedDate,
            }, where: x => x.Id == mailRun.Id, token: token);

            log.LogInformation("Generated {Count} in {Day} MailRun {Id} for {Tag}",
                emails, day, mailRun.Id, tag);

            jobs.EnqueueCommand<SendMailRunCommand>(new SendMailRun {
                Id = mailRun.Id
            });
        }

        log.LogInformation("Generated {Count} MailRuns for {Day}", mailRuns, day);
    }
}
