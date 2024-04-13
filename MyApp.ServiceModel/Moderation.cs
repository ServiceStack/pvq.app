using ServiceStack;

namespace MyApp.ServiceModel;

[ValidateHasRole(Roles.Moderator)]
public class Sync : IGet, IReturn<StringResponse>
{
    public List<string>? Tasks { get; set; }
}
