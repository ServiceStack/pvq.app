using CreatorKit.ServiceInterface;
using CreatorKit.ServiceModel;
using CreatorKit.ServiceModel.Types;
using Microsoft.Extensions.Logging;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.CreatorKit;

[Tag(Tags.CreatorKit)]
[Worker(Databases.CreatorKit)]
public class SendMailRunCommand(
    ILogger<SendMailRunCommand> logger,
    IBackgroundJobs jobs,
    IDbConnectionFactory dbFactory,
    EmailProvider emailProvider)
    : SyncCommand<SendMailRun>
{
    protected override void Run(SendMailRun request)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        var job = Request.GetBackgroundJob();
        using var db = dbFactory.Open(Databases.CreatorKit);
        var msgIdsToSend = db.Column<int>(db.From<MailMessageRun>()
            .Where(x => x.MailRunId == request.Id && x.CompletedDate == null && x.StartedDate == null)
            .Select(x => x.Id));

        if (msgIdsToSend.Count == 0)
        {
            log.LogInformation("No remaining unsent Messages to send for MailRun {Id}", request.Id);
            return;
        }
        
        db.UpdateOnly(() => new MailRun { SentDate = DateTime.UtcNow }, 
            where:x => x.Id == request.Id && x.SentDate == null);

        log.LogInformation("Sending {Count} Messages for MailRun {Id}", msgIdsToSend.Count, request.Id);

        var i = 0;
        foreach (var msgId in msgIdsToSend)
        {
            try
            {
                var progress = ++i / (double)msgIdsToSend.Count * 0.95 + 0.05;
                jobs.UpdateJobStatus(new(job, progress:progress, log:$"Sending Message {msgId} for MailRun {request.Id}"));

                var msg = db.SingleById<MailMessageRun>(msgId);
                if (msg.CompletedDate != null)
                { 
                    log.LogWarning("MailMessageRun {Id} has already been sent", msg.Id);
                    continue;
                }

                // ensure message is only sent once
                if (db.UpdateOnly(() => new MailMessageRun { StartedDate = DateTime.UtcNow },
                        where: x => x.Id == request.Id && x.StartedDate == null) == 1)
                {
                    try
                    {
                        emailProvider.Send(msg.Message);
                        
                        db.UpdateOnly(() => new MailMessageRun { CompletedDate = DateTime.UtcNow },
                            where: x => x.Id == request.Id);
                    }
                    catch (Exception e)
                    {
                        var error = e.ToResponseStatus();
                        db.UpdateOnly(() => new MailMessageRun { Error = error },
                            where: x => x.Id == request.Id);
                    }
                }
            }
            catch (Exception e)
            {
                var error = e.ToResponseStatus();
                db.UpdateOnly(() => new MailMessageRun
                {
                    Error = error
                }, where: x => x.Id == msgId);
                
                log.LogError(e, "Error sending MailMessageRun {Id}: {Message}", msgId, e.Message);
            }
        }
    }
}
