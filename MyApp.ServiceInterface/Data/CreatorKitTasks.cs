using CreatorKit.ServiceInterface;
using CreatorKit.ServiceModel.Types;
using ServiceStack;

namespace MyApp.Data;

public class SendMailMessages
{
    public List<int>? Ids { get; set; }
    public List<MailMessage>? Messages { get; set; }
}

public class CreatorKitTasks
{
    [Command<SendMessagesCommand>]
    public SendMailMessages? SendMessages { get; set; }
}
