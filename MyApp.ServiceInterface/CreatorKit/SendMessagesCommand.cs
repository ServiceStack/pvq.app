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
    : SyncCommand<SendMessages>
{
    protected override void Run(SendMessages request)
    {
        using var db = dbFactory.Open(Databases.CreatorKit);

        foreach (var msg in request.Messages.Safe())
        {
            if (msg.CompletedDate != null)
                throw new Exception($"Message {msg.Id} has already been sent");

            msg.Id = (int) db.Insert(msg, selectIdentity:true);

            // ensure message is only sent once
            if (db.UpdateOnly(() => new MailMessage { StartedDate = DateTime.UtcNow, Draft = false },
                where: x => x.Id == msg.Id && x.StartedDate == null) == 1)
            {
                try
                {
                    emailProvider.Send(msg.Message);
                }
                catch (Exception e)
                {
                    var error = e.ToResponseStatus();
                    db.UpdateOnly(() => new MailMessage { Error = error },
                        where: x => x.Id == msg.Id);
                    throw;
                }

                db.UpdateOnly(() => new MailMessage { CompletedDate = DateTime.UtcNow },
                    where: x => x.Id == msg.Id);
            }
        }
    }
}
