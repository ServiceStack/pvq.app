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

public class NewComment
{
    // Post or AnswerId
    public string RefId { get; set; }
    public Comment Comment { get; set; }
}

public class DeletePost
{
    public required List<int> Ids { get; set; }
}

public class CreatePostJobs
{
    public required List<PostJob> PostJobs { get; set; }
}

public class CompletePostJobs
{
    public required List<int> Ids { get; set; }
}

public class AnswerAddedToPost
{
    public required int Id { get; set; }
}
public class UpdateReputations {}

[Tag(Tag.Tasks)]
[ExcludeMetadata]
[Restrict(InternalOnly = true)]
public class DbWrites
{
    public Vote? CreatePostVote { get; set; }
    public Post? CreatePost { get; set; }
    public Post? UpdatePost { get; set; }
    public DeletePost? DeletePost { get; set; }
    public CreatePostJobs? CreatePostJobs { get; set; }
    public StartJob? StartJob { get; set; }
    public Post? CreateAnswer { get; set; }
    public AnswerAddedToPost? AnswerAddedToPost { get; set; }
    public NewComment? NewComment { get; set; }
    public DeleteComment? DeleteComment { get; set; }
    public CompletePostJobs? CompletePostJobs { get; set; }
    public FailJob? FailJob { get; set; }
    public ApplicationUser? UserRegistered { get; set; }
    public ApplicationUser? UserSignedIn { get; set; }
    public UpdateReputations? UpdateReputations { get; set; }
    public MarkAsRead? MarkAsRead { get; set; }
    public Notification? CreateNotification { get; set; }
}

[Tag(Tag.Tasks)]
[ExcludeMetadata]
[Restrict(InternalOnly = true)]
public class SearchTasks
{
    public int? AddPostToIndex { get; set; }
    public int? DeletePost { get; set; }
}

[Tag(Tag.Tasks)]
[ExcludeMetadata]
[Restrict(InternalOnly = true)]
public class ViewCommands : IGet, IReturn<CommandResult[]>
{
    public bool? Clear { get; set; }
}
