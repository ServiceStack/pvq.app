using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[Tag(Tag.Tasks)]
[ExcludeMetadata]
[Restrict(InternalOnly = true)]
public class DiskTasks : IReturnVoid
{
    public SaveFile? SaveFile { get; set; }
    public List<string>? CdnDeleteFiles { get; set; }
}
public class SaveFile
{
    public string FilePath { get; set; }
    public Stream Stream { get; set; }
}

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
