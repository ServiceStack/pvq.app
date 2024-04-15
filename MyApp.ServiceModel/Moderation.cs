using ServiceStack;

namespace MyApp.ServiceModel;

[ValidateHasRole(Roles.Moderator)]
public class Sync : IGet, IReturn<StringResponse>
{
    public List<string>? Tasks { get; set; }
}

[ValidateHasRole(Roles.Moderator)]
public class GenerateMeta : IGet, IReturn<QuestionAndAnswers>
{
    [ValidateGreaterThan(0)]
    public int Id { get; set; }
}
