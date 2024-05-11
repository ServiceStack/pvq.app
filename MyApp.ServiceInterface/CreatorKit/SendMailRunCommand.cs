using CreatorKit.ServiceInterface;
using CreatorKit.ServiceModel;
using CreatorKit.ServiceModel.Types;
using Microsoft.Extensions.Logging;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.CreatorKit;

public class SendMailRunCommand(
    ILogger<SendMailRunCommand> log,
    IDbConnectionFactory dbFactory,
    EmailProvider emailProvider)
    : IAsyncCommand<SendMailRun>
{
    public async Task ExecuteAsync(SendMailRun request)
    {
        using var db = HostContext.AppHost.GetDbConnection(Databases.CreatorKit);
        var msgIdsToSend = await db.ColumnAsync<int>(db.From<MailMessageRun>()
            .Where(x => x.MailRunId == request.Id && x.CompletedDate == null && x.StartedDate == null)
            .Select(x => x.Id));

        if (msgIdsToSend.Count == 0)
        {
            log.LogInformation("No remaining unsent Messages to send for MailRun {Id}", request.Id);
            return;
        }
        
        await db.UpdateOnlyAsync(() => new MailRun { SentDate = DateTime.UtcNow }, 
            where:x => x.Id == request.Id && x.SentDate == null);

        log.LogInformation("Sending {Count} Messages for MailRun {Id}", msgIdsToSend.Count, request.Id);

        foreach (var msgId in msgIdsToSend)
        {
            try
            {
                var msg = await db.SingleByIdAsync<MailMessageRun>(msgId);
                if (msg.CompletedDate != null)
                { 
                    log.LogWarning("MailMessageRun {Id} has already been sent", msg.Id);
                    continue;
                }

                // ensure message is only sent once
                if (await db.UpdateOnlyAsync(() => new MailMessageRun { StartedDate = DateTime.UtcNow },
                        where: x => x.Id == request.Id && x.StartedDate == null) == 1)
                {
                    try
                    {
                        emailProvider.Send(msg.Message);
                        
                        await db.UpdateOnlyAsync(() => new MailMessageRun { CompletedDate = DateTime.UtcNow },
                            where: x => x.Id == request.Id);
                    }
                    catch (Exception e)
                    {
                        var error = e.ToResponseStatus();
                        await db.UpdateOnlyAsync(() => new MailMessageRun { Error = error },
                            where: x => x.Id == request.Id);
                    }
                }
            }
            catch (Exception e)
            {
                var error = e.ToResponseStatus();
                await db.UpdateOnlyAsync(() => new MailMessageRun
                {
                    Error = error
                }, where: x => x.Id == msgId);
                
                log.LogError(e, "Error sending MailMessageRun {Id}: {Message}", msgId, e.Message);
            }
        }
    }
}
