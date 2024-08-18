using CreatorKit.ServiceModel.Types;
using MyApp.Data;
using MyApp.ServiceInterface;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;

namespace CreatorKit.ServiceInterface;

public class SendMessages
{
    public List<int>? Ids { get; set; }
    public List<MailMessage>? Messages { get; set; }
}

[Tag(Tags.CreatorKit)]
[Worker(Databases.CreatorKit)]
public class SendMessagesCommand(IDbConnectionFactory dbFactory, EmailProvider emailProvider) 
    : AsyncCommand<SendMessages>
{
    protected override async Task RunAsync(SendMessages request, CancellationToken token)
    {
        using var db = await dbFactory.OpenDbConnectionAsync(Databases.CreatorKit, token: token);

        foreach (var msg in request.Messages.Safe())
        {
            if (msg.CompletedDate != null)
                throw new Exception($"Message {msg.Id} has already been sent");

            msg.Id = (int) await db.InsertAsync(msg, selectIdentity:true, token: token);

            // ensure message is only sent once
            if (await db.UpdateOnlyAsync(() => new MailMessage { StartedDate = DateTime.UtcNow, Draft = false },
                    where: x => x.Id == msg.Id && (x.StartedDate == null), token: token) == 1)
            {
                try
                {
                    emailProvider.Send(msg.Message);
                }
                catch (Exception e)
                {
                    var error = e.ToResponseStatus();
                    await db.UpdateOnlyAsync(() => new MailMessage { Error = error },
                        where: x => x.Id == msg.Id, token: token);
                    throw;
                }

                await db.UpdateOnlyAsync(() => new MailMessage { CompletedDate = DateTime.UtcNow },
                    where: x => x.Id == msg.Id, token: token);
            }
        }
    }
}
