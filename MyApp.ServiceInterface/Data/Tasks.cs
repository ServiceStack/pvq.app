using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.Data;

[ExcludeMetadata]
[Restrict(InternalOnly = true)]
public class SendEmail : IReturn<EmptyResponse>
{
    public string To { get; set; }
    public string? ToName { get; set; }
    public string Subject { get; set; }
    public string? BodyText { get; set; }
    public string? BodyHtml { get; set; }
}

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
    public Stream? Stream { get; set; }
    public string? Text { get; set; }
    public byte[]? Bytes { get; set; }
}

[Tag(Tag.Tasks)]
[ExcludeMetadata]
[Restrict(InternalOnly = true)]
public class AnalyticsTasks
{
    public SearchView? RecordSearchView { get; set; }
    public PostView? RecordPostView { get; set; }
    public int? DeletePost { get; set; }
}

public class StartJob
{
    public int Id { get; set; }
    public string? Worker { get; set; }
    public string? WorkerIp { get; set; }
}

[Tag(Tag.Tasks)]
[ExcludeMetadata]
[Restrict(InternalOnly = true)]
public class DbWrites
{
    public Vote? CreatePostVote { get; set; }
    public Post? CreatePost { get; set; }
    public Post? UpdatePost { get; set; }
    public int? DeletePost { get; set; }
    public List<PostJob>? CreatePostJobs { get; set; }
    public StartJob? StartJob { get; set; }
    public int? AnswerAddedToPost { get; set; }
    public List<int>? CompleteJobIds { get; set; }
    public FailJob? FailJob { get; set; }
    public ApplicationUser? UserRegistered { get; set; }
    public ApplicationUser? UserSignedIn { get; set; }
    public bool? UpdateReputations { get; set; }
}

[Tag(Tag.Tasks)]
[ExcludeMetadata]
[Restrict(InternalOnly = true)]
public class SearchTasks
{
    public int? AddPostToIndex { get; set; }
    public int? DeletePost { get; set; }
}
