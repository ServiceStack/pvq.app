using ServiceStack;
using CreatorKit.ServiceModel.Types;
using MyApp.Data;
using MyApp.ServiceInterface;
using MyApp.ServiceModel;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace CreatorKit.ServiceInterface;

public class CreatorKitTasksServices : Service
{
    public object Any(CreatorKitTasks request) => Request.ExecuteCommandsAsync(request);
}

public class SendMessagesCommand(IDbConnectionFactory dbFactory, EmailProvider emailProvider) : IAsyncCommand<SendMailMessages>
{
    public async Task ExecuteAsync(SendMailMessages request)
    {
        using var db = await dbFactory.OpenDbConnectionAsync(Databases.CreatorKit);

        foreach (var msg in request.Messages.Safe())
        {
            if (msg.CompletedDate != null)
                throw new Exception($"Message {msg.Id} has already been sent");

            msg.Id = (int) await db.InsertAsync(msg, selectIdentity:true);

            // ensure message is only sent once
            if (await db.UpdateOnlyAsync(() => new MailMessage { StartedDate = DateTime.UtcNow, Draft = false },
                    where: x => x.Id == msg.Id && (x.StartedDate == null)) == 1)
            {
                try
                {
                    emailProvider.Send(msg.Message);
                }
                catch (Exception e)
                {
                    var error = e.ToResponseStatus();
                    await db.UpdateOnlyAsync(() => new MailMessage { Error = error },
                        where: x => x.Id == msg.Id);
                    throw;
                }

                await db.UpdateOnlyAsync(() => new MailMessage { CompletedDate = DateTime.UtcNow },
                    where: x => x.Id == msg.Id);
            }
        }
    }
}