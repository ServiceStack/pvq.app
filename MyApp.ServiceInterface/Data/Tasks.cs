using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.Data;

[Tag(Tag.Tasks)]
[Restrict(RequestAttributes.MessageQueue), ExcludeMetadata]
public class SendEmail : IReturn<EmptyResponse>
{
    public string To { get; set; }
    public string? ToName { get; set; }
    public string Subject { get; set; }
    public string? BodyText { get; set; }
    public string? BodyHtml { get; set; }
}

[Tag(Tag.Tasks)]
[Restrict(RequestAttributes.MessageQueue), ExcludeMetadata]
public class DiskTasks : IReturnVoid
{
    public SaveFile? SaveFile { get; set; }
    public List<string>? CdnDeleteFiles { get; set; }
    public Post? SaveQuestion { get; set; }
}
public class SaveFile
{
    public string FilePath { get; set; }
    public Stream? Stream { get; set; }
    public string? Text { get; set; }
    public byte[]? Bytes { get; set; }
}

[Tag(Tag.Tasks)]
[Restrict(RequestAttributes.MessageQueue), ExcludeMetadata]
public class AnalyticsTasks
{
    public SearchStat? CreateSearchStat { get; set; }
    public PostStat? CreatePostStat { get; set; }
    public int? DeletePost { get; set; }
}

public class DeletePosts
{
    public required List<int> Ids { get; set; }
}

public class DeleteAnswers
{
    public required List<string> Ids { get; set; }
}

public class MarkPostAsRead
{
    public int PostId { get; set; }
    public string UserName { get; set; }
}
