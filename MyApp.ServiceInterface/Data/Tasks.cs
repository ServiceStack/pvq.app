using MyApp.ServiceInterface;
using MyApp.ServiceInterface.App;
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
[Restrict(RequestAttributes.MessageQueue), ExcludeMetadata]
public class DbWrites : IGet, IReturn<EmptyResponse>
{
    [Command(typeof(CreatePostVotesCommand))]
    public Vote? CreatePostVote { get; set; }
    
    [Command(typeof(CreatePostCommand))]
    public Post? CreatePost { get; set; }
    
    [Command(typeof(UpdatePostCommand))]
    public Post? UpdatePost { get; set; }
    
    [Command(typeof(DeletePostCommand))]
    public DeletePost? DeletePost { get; set; }
    
    [Command(typeof(CreatePostJobsCommand))]
    public CreatePostJobs? CreatePostJobs { get; set; }
    
    [Command(typeof(StartJobCommand))]
    public StartJob? StartJob { get; set; }
    
    [Command(typeof(CreateAnswerCommand))]
    public Post? CreateAnswer { get; set; }
    
    [Command(typeof(AnswerAddedToPostCommand))]
    public AnswerAddedToPost? AnswerAddedToPost { get; set; }
    
    [Command(typeof(NewCommentCommand))]
    public NewComment? NewComment { get; set; }
    
    [Command(typeof(DeleteCommentCommand))]
    public DeleteComment? DeleteComment { get; set; }
    
    [Command(typeof(CompletePostJobsCommand))]
    public CompletePostJobs? CompletePostJobs { get; set; }
    
    [Command(typeof(FailJobCommand))]
    public FailJob? FailJob { get; set; }
    
    [Command(typeof(UpdateReputationsCommand))]
    public UpdateReputations? UpdateReputations { get; set; }
    
    [Command(typeof(MarkAsReadCommand))]
    public MarkAsRead? MarkAsRead { get; set; }
    
    [Command(typeof(CreateNotificationCommand))]
    public Notification? CreateNotification { get; set; }
}

public class RenderHome
{
    public string? Tab { get; set; }
    public List<Post> Posts { get; set; }
}

public class RegenerateMeta
{
    public int? IfPostModified { get; set; }
    public int? ForPost { get; set; }
}

public class RenderComponent : IReturnVoid
{
    public RegenerateMeta? RegenerateMeta { get; set; }
    
    public QuestionAndAnswers? Question { get; set; }
    
    public RenderHome? Home { get; set; }
}

[Tag(Tag.Tasks)]
[ExcludeMetadata]
[Restrict(InternalOnly = true)]
public class SearchTasks
{
    public int? AddPostToIndex { get; set; }
    public int? DeletePost { get; set; }
}


