using MyApp.ServiceInterface;
using MyApp.ServiceInterface.AiServer;
using MyApp.ServiceInterface.App;
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

public class MarkPostAsRead
{
    public int PostId { get; set; }
    public string UserName { get; set; }
}

[Tag(Tag.Tasks)]
[Restrict(RequestAttributes.MessageQueue), ExcludeMetadata]
public class DbWrites : IGet, IReturn<EmptyResponse>
{
    [Command<CreatePostVoteCommand>]
    public Vote? CreatePostVote { get; set; }
    
    [Command<CreateCommentVoteCommand>]
    public Vote? CreateCommentVote { get; set; }
    
    [Command<CreatePostCommand>]
    public Post? CreatePost { get; set; }
    
    [Command<UpdatePostCommand>]
    public Post? UpdatePost { get; set; }
    
    [Command<DeletePostCommand>]
    public DeletePost? DeletePost { get; set; }
    
    [Command<CreatePostJobsCommand>]
    public CreatePostJobs? CreatePostJobs { get; set; }
    
    [Command<StartJobCommand>]
    public StartJob? StartJob { get; set; }
    
    [Command<CreateAnswerCommand>]
    public Post? CreateAnswer { get; set; }
    
    [Command<AnswerAddedToPostCommand>]
    public AnswerAddedToPost? AnswerAddedToPost { get; set; }
    
    [Command<NewCommentCommand>]
    public NewComment? NewComment { get; set; }
    
    [Command<DeleteCommentCommand>]
    public DeleteComment? DeleteComment { get; set; }
    
    [Command<CompletePostJobsCommand>]
    public CompletePostJobs? CompletePostJobs { get; set; }
    
    [Command<FailJobCommand>]
    public FailJob? FailJob { get; set; }
    
    [Command<UpdateReputationsCommand>]
    public UpdateReputations? UpdateReputations { get; set; }
    
    [Command<MarkAsReadCommand>]
    public MarkAsRead? MarkAsRead { get; set; }
    
    [Command<CreateNotificationCommand>]
    public Notification? CreateNotification { get; set; }
    
    [Command<CreateFlagCommand>]
    public Flag? CreateFlag { get; set; }
    
    [Command<ImportQuestionCommand>]
    public ImportQuestion? ImportQuestion { get; set; }
    
    [Command<MarkPostAsReadCommand>]
    public MarkPostAsRead? MarkPostAsRead { get; set; }
    
    [Command<PostSubscriptionsCommand>]
    public PostSubscriptions? PostSubscriptions { get; set; }
    
    [Command<TagSubscriptionsCommand>]
    public TagSubscriptions? TagSubscriptions { get; set; }
}

public class AiServerTasks
{
    [Command<CreateRankAnswerTaskCommand>]
    public CreateRankAnswerTask? CreateRankAnswerTask { get; set; } 
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
[Restrict(RequestAttributes.MessageQueue), ExcludeMetadata]
public class SearchTasks
{
    public int? AddPostToIndex { get; set; }
    public int? DeletePost { get; set; }
}
