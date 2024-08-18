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
    ILogger<SendMailRunCommand> log,
    IBackgroundJobs jobs,
    IDbConnectionFactory dbFactory,
    EmailProvider emailProvider)
    : AsyncCommand<SendMailRun>
{
    protected override async Task RunAsync(SendMailRun request, CancellationToken token)
    {
        var job = Request.GetBackgroundJob();
        using var db = await dbFactory.OpenDbConnectionAsync(Databases.CreatorKit, token:token);
        var msgIdsToSend = await db.ColumnAsync<int>(db.From<MailMessageRun>()
            .Where(x => x.MailRunId == request.Id && x.CompletedDate == null && x.StartedDate == null)
            .Select(x => x.Id), token: token);

        if (msgIdsToSend.Count == 0)
        {
            log.LogInformation("No remaining unsent Messages to send for MailRun {Id}", request.Id);
            jobs.UpdateJobStatus(new(job, log:$"No remaining unsent Messages to send for MailRun {request.Id}"));
            return;
        }
        
        await db.UpdateOnlyAsync(() => new MailRun { SentDate = DateTime.UtcNow }, 
            where:x => x.Id == request.Id && x.SentDate == null, token: token);

        log.LogInformation("Sending {Count} Messages for MailRun {Id}", msgIdsToSend.Count, request.Id);
        jobs.UpdateJobStatus(new(job, progress:0.05, log:$"Sending {msgIdsToSend.Count} Messages for MailRun {request.Id}"));

        var i = 0;
        foreach (var msgId in msgIdsToSend)
        {
            try
            {
                var progress = ++i / (double)msgIdsToSend.Count * 0.95 + 0.05;
                jobs.UpdateJobStatus(new(job, progress:progress, log:$"Sending Message {msgId} for MailRun {request.Id}"));

                var msg = await db.SingleByIdAsync<MailMessageRun>(msgId, token: token);
                if (msg.CompletedDate != null)
                { 
                    log.LogWarning("MailMessageRun {Id} has already been sent", msg.Id);
                    jobs.UpdateJobStatus(new(job, log:$"MailMessageRun {msg.Id} has already been sent"));
                    continue;
                }

                // ensure message is only sent once
                if (await db.UpdateOnlyAsync(() => new MailMessageRun { StartedDate = DateTime.UtcNow },
                        where: x => x.Id == request.Id && x.StartedDate == null, token: token) == 1)
                {
                    try
                    {
                        emailProvider.Send(msg.Message);
                        
                        await db.UpdateOnlyAsync(() => new MailMessageRun { CompletedDate = DateTime.UtcNow },
                            where: x => x.Id == request.Id, token: token);
                    }
                    catch (Exception e)
                    {
                        var error = e.ToResponseStatus();
                        await db.UpdateOnlyAsync(() => new MailMessageRun { Error = error },
                            where: x => x.Id == request.Id, token: token);
                    }
                }
            }
            catch (Exception e)
            {
                var error = e.ToResponseStatus();
                await db.UpdateOnlyAsync(() => new MailMessageRun
                {
                    Error = error
                }, where: x => x.Id == msgId, token: token);
                
                log.LogError(e, "Error sending MailMessageRun {Id}: {Message}", msgId, e.Message);
                jobs.UpdateJobStatus(new(job, log:$"Error sending MailMessageRun {msgId}: {e.Message}"));
            }
        }
    }

}
