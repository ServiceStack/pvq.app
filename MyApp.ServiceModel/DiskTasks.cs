using ServiceStack;

namespace MyApp.ServiceModel;

[Tag(Tag.Tasks)]
[ValidateHasRole(Roles.Moderator)]
public class DeleteCdnFilesMq
{
    public List<string> Files { get; set; }
}

[Tag(Tag.Tasks)]
[ValidateHasRole(Roles.Moderator)]
public class GetCdnFile
{
    public string File { get; set; }
}

[Tag(Tag.Tasks)]
[ValidateHasRole(Roles.Moderator)]
public class DeleteCdnFile : IReturnVoid
{
    public string File { get; set; }
}
