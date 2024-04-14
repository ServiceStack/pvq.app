using MyApp.ServiceModel;
using ServiceStack;

namespace CreatorKit.ServiceModel;

[ValidateHasRole(Roles.Moderator)]
public class SendNewAnswerEmail : IGet, IReturn<StringResponse>
{
    public required string UserName { get; set; }
    public required string AnswerId { get; set; }
}
