using CreatorKit.ServiceInterface;
using CreatorKit.ServiceModel;
using CreatorKit.ServiceModel.Types;
using MyApp.ServiceInterface.CreatorKit;
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
    
    [Command<SendMailRunCommand>]
    public SendMailRun? SendMailRun { get; set; }
}