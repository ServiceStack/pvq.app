// Complete declarative AutoQuery services for Bookings CRUD example:
// https://docs.servicestack.net/autoquery-crud-bookings

using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[Icon(Svg = Icons.Post)]
[Description("StackOverflow Question")]
[Notes("A StackOverflow Question Post")]
public class Post : IMeta
{
    public int Id { get; set; }

    [Required] public int PostTypeId { get; set; }

    public int? AcceptedAnswerId { get; set; }

    public int? ParentId { get; set; }

    public int Score { get; set; }

    public int? ViewCount { get; set; }

    public string Title { get; set; }

    public int? FavoriteCount { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime LastActivityDate { get; set; }

    public DateTime? LastEditDate { get; set; }

    public int? LastEditorUserId { get; set; }

    public int? OwnerUserId { get; set; }

    public List<string> Tags { get; set; }

    public string Slug { get; set; }

    public string Summary { get; set; }
    
    public DateTime? RankDate { get; set; }
    
    public int? AnswerCount { get; set; }

    public string? CreatedBy { get; set; }
    
    public string? ModifiedBy { get; set; }
    
    public string? Body { get; set; }

    public string? ModifiedReason { get; set; }
    
    public DateTime? LockedDate { get; set; }

    public string? LockedReason { get; set; }
    
    public string? RefId { get; set; }

    public string? RefUrn { get; set; }

    public Dictionary<string, string>? Meta { get; set; }

    public string GetRefId() => RefId ?? (PostTypeId == 1 ? $"{Id}" : $"{Id}-{CreatedBy}");
}

public static class PostUtils
{
    public static string GetPostType(this Post post) => post.PostTypeId switch
    {
        1 => "Question",
        2 => "Answer",
        3 => "Wiki",
        4 => "TagWiki",
        5 => "ModeratorNomination",
        6 => "WikiPlaceholder",
        7 => "PrivilegeWiki",
        8 => "TagWikiExcerpt",
        _ => "Unknown"
    };
}

public class PostJob
{
    [AutoIncrement]
    public int Id { get; set; }
    public int PostId { get; set; }
    public string Model { get; set; }
    public string Title { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? StartedDate { get; set; }
    public string? Worker { get; set; }
    public string? WorkerIp { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
}

public class CheckPostJobs : IGet, IReturn<CheckPostJobsResponse>
{
    public string WorkerId { get; set; }
    public List<string> Models { get; set; }
}
public class CheckPostJobsResponse
{
    public List<PostJob> Results { get; set; }
}

public class GetNextJobs : IGet, IReturn<GetNextJobsResponse>
{
    [ValidateNotEmpty]
    public List<string> Models { get; set; } = [];
    public string? Worker { get; set; }
    public int? Take { get; set; }
}
public class GetNextJobsResponse
{
    public List<PostJob> Results { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[ValidateHasRole(Roles.Moderator)]
public class ViewModelQueues : IGet, IReturn<ViewModelQueuesResponse>
{
    public List<string> Models { get; set; } = [];
}

public class ViewModelQueuesResponse
{
    public List<PostJob> Jobs { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[ValidateHasRole(Roles.Moderator)]
public class RestoreModelQueues : IGet, IReturn<StringsResponse>
{
    public bool? RestoreFailedJobs { get; set; }
}

[ValidateHasRole(Roles.Moderator)]
public class FailJob : IPost, IReturnVoid
{
    public int Id { get; set; }
    [ValidateNotEmpty]
    public required string Error { get; set; }
}


public class QueryPosts : QueryDb<Post>
{
}

public class PostFts
{
    [Alias("rowid")] public int Id { get; set; }
    public string RefId { get; set; }
    public string UserName { get; set; }
    public string Body { get; set; }
    public string? Tags { get; set; }
    public DateTime ModifiedDate { get; set; }
}

public class Comment
{
    public string Body { get; set; }
    public long Created { get; set; } //timestamp ms 
    public string CreatedBy { get; set; }
    public int? UpVotes { get; set; }
    public int? Reports { get; set; }
}

public class QuestionAndAnswers
{
    public int Id => Post.Id;
    public Post Post { get; set; }
    public Meta? Meta { get; set; }
    public List<Post> Answers { get; set; } = [];

    public int ViewCount => Post.ViewCount + Meta?.StatTotals.Find(x => x.Id == $"{Id}")?.ViewCount ?? 0;
    
    public int QuestionScore => Meta?.StatTotals.Find(x => x.Id == $"{Id}")?.GetScore() ?? Post.Score;
    
    public int GetAnswerScore(string refId) => Meta?.StatTotals.Find(x => x.Id == refId)?.GetScore() ?? 0;

    public List<Comment> QuestionComments => Meta?.Comments.TryGetValue($"{Id}", out var comments) == true
        ? comments
        : [];
    
    public List<Comment> GetAnswerComments(string refId) => Meta?.Comments.TryGetValue($"{refId}", out var comments) == true
        ? comments
        : [];
}

public class AdminData : IGet, IReturn<AdminDataResponse>
{
}

public class PageStats
{
    public string Label { get; set; }
    public int Total { get; set; }
}

public class AdminDataResponse
{
    public List<PageStats> PageStats { get; set; }
}

[ValidateIsAuthenticated]
public class UserPostData : IGet, IReturn<UserPostDataResponse>
{
    public int PostId { get; set; }
}

public class UserPostDataResponse
{
    public bool Watching { get; set; }
    public HashSet<string> UpVoteIds { get; set; } = [];
    public HashSet<string> DownVoteIds { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

[ValidateIsAuthenticated]
public class PostVote : IReturnVoid
{
    public string RefId { get; set; }
    public bool? Up { get; set; }
    public bool? Down { get; set; }
}

[ValidateIsAuthenticated]
public class CommentVote : IReturnVoid
{
    public string RefId { get; set; }
    public bool? Up { get; set; }
    public bool? Down { get; set; }
}

[ValidateHasRole(Roles.Moderator)]
public class CreateWorkerAnswer : IPost, IReturn<IdResponse>
{
    public int PostId { get; set; }
    [ValidateNotEmpty]
    public string Model { get; set; }
    public string Json { get; set; }
    public int? PostJobId { get; set; }
}

[ValidateHasRole(Roles.Moderator)]
public class RankAnswers : IPost, IReturn<IdResponse>
{
    [ValidateGreaterThan(0)]
    public int PostId { get; set; }
    
    /// <summary>
    /// Model used to rank the answers
    /// </summary>
    public string Model { get; set; }
    
    public Dictionary<string,int> ModelVotes { get; set; }
    
    public int? PostJobId { get; set; }
}

[ValidateIsAuthenticated]
public class GetQuestion : IGet, IReturn<GetQuestionResponse>
{
    public int Id { get; set; }
}
public class GetQuestionResponse
{
    public required Post Result { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[ValidateIsAuthenticated]
public class GetQuestionBody : IGet, IReturn<string>
{
    public int Id { get; set; }
}

public class GetQuestionFile : IGet, IReturn<string>
{
    public int Id { get; set; }
}

public class FindSimilarQuestions : IGet, IReturn<FindSimilarQuestionsResponse>
{
    [ValidateNotEmpty, ValidateMinimumLength(20)]
    public string Text { get; set; }
}

public class FindSimilarQuestionsResponse
{
    public List<Post> Results { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[ValidateIsAuthenticated]
public class AskQuestion : IPost, IReturn<AskQuestionResponse>
{
    [ValidateNotEmpty, ValidateMinimumLength(20), ValidateMaximumLength(120)]
    [Input(Type = "text", Help = "A summary of what your main question is asking"), FieldCss(Field="col-span-12")]
    public required string Title { get; set; }
    
    [ValidateNotEmpty, ValidateMinimumLength(30), ValidateMaximumLength(32768)]
    [Input(Type="MarkdownInput", Help = "Include all information required for someone to identity and resolve your exact question"), FieldCss(Field="col-span-12", Input="h-60")]
    public required string Body { get; set; }
    
    [ValidateNotEmpty, ValidateMinimumLength(2, Message = "At least 1 tag required"), ValidateMaximumLength(120)]
    [Input(Type = "tag", Help = "Up to 5 tags relevant to your question"), FieldCss(Field="col-span-12")]
    public required List<string> Tags { get; set; }
    
    [Input(Type="hidden")]
    public string? RefId { get; set; }
    
    [Input(Type="hidden")]
    public string? RefUrn { get; set; }
}
public class AskQuestionResponse
{
    public int Id { get; set; }
    public string Slug { get; set; }
    public string? RedirectTo { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[ValidateIsAuthenticated]
public class UpdateQuestion : IPost, IReturn<UpdateQuestionResponse>
{
    [Input(Type="hidden")]
    [ValidateGreaterThan(0)]
    public int Id { get; set; }
    
    [ValidateNotEmpty, ValidateMinimumLength(20), ValidateMaximumLength(120)]
    [Input(Type = "text", Help = "A summary of what your main question is asking"), FieldCss(Field="col-span-12")]
    public required string Title { get; set; }
    
    [ValidateNotEmpty, ValidateMinimumLength(30), ValidateMaximumLength(32768)]
    [Input(Type="MarkdownInput", Help = "Include all information required for someone to identity and resolve your exact question"), FieldCss(Field="col-span-12", Input="h-60")]
    public required string Body { get; set; }
    
    [ValidateNotEmpty, ValidateMinimumLength(2, Message = "At least 1 tag required"), ValidateMaximumLength(120)]
    [Input(Type = "tag", Help = "Up to 5 tags relevant to your question"), FieldCss(Field="col-span-12")]
    public required List<string> Tags { get; set; }

    [Input(Type="text", Placeholder="Short summary of this edit (e.g. corrected spelling, grammar, improved formatting)"),FieldCss(Field = "col-span-12")]
    [ValidateNotEmpty, ValidateMinimumLength(4)]
    public required string EditReason { get; set; }
}
public class UpdateQuestionResponse
{
    public required Post Result { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[ValidateIsAuthenticated]
[Description("Your Answer")]
public class AnswerQuestion : IPost, IReturn<AnswerQuestionResponse>
{
    [Input(Type="hidden")]
    [ValidateGreaterThan(0)]
    public int PostId { get; set; }
    
    [ValidateNotEmpty, ValidateMinimumLength(30), ValidateMaximumLength(32768)]
    [Input(Type="MarkdownInput", Label=""), FieldCss(Field="col-span-12", Input="h-60")]
    public required string Body { get; set; }
    
    [Input(Type="hidden")]
    public string? RefId { get; set; }
}
public class AnswerQuestionResponse
{
    public ResponseStatus? ResponseStatus { get; set; }
}

[ValidateIsAuthenticated]
[Description("Your Answer")]
public class UpdateAnswer : IPost, IReturn<UpdateAnswerResponse>
{
    [Input(Type="hidden")]
    [ValidateNotEmpty]
    public required string Id { get; set; }
    
    [ValidateNotEmpty, ValidateMinimumLength(30), ValidateMaximumLength(32768)]
    [Input(Type="MarkdownInput", Label=""), FieldCss(Field="col-span-12", Input="h-60")]
    public required string Body { get; set; }

    [Input(Type="text", Placeholder="Short summary of this edit (e.g. corrected spelling, grammar, improved formatting)"),FieldCss(Field = "col-span-12")]
    [ValidateNotEmpty, ValidateMinimumLength(4)]
    public required string EditReason { get; set; }
}
public class UpdateAnswerResponse
{
    public ResponseStatus? ResponseStatus { get; set; }
}

[ValidateIsAuthenticated]
public class CreateComment : IPost, IReturn<CommentsResponse>
{
    [ValidateNotEmpty]
    public required string Id { get; set; }

    [ValidateNotEmpty, ValidateMinimumLength(15)]
    public required string Body { get; set; }
}

public class CommentsResponse
{
    public List<Comment> Comments { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[ValidateIsAuthenticated]
public class DeleteComment : IPost, IReturn<CommentsResponse>
{
    [ValidateNotEmpty]
    public required string Id { get; set; }
    [ValidateNotEmpty]
    public string CreatedBy { get; set; }
    [ValidateGreaterThan(0)]
    public long Created { get; set; }
}

public class GetMeta : IGet, IReturn<Meta>
{
    [ValidateNotEmpty]
    public required string Id { get; set; }
}

public class PreviewMarkdown : IPost, IReturn<string>
{
    public string Markdown { get; set; }
}

public class GetAnswerBody : IGet, IReturn<string>
{
    public string Id { get; set; }
}

[ValidateHasRole(Roles.Moderator)]
public class DeleteQuestion : IGet, IReturn<EmptyResponse>
{
    [ValidateGreaterThan(0)]
    public int Id { get; set; }
    
    public string? ReturnUrl { get; set; }
}

public class GetRequestInfo : IGet, IReturn<string> {}

public class GetUserReputations : IGet, IReturn<GetUserReputationsResponse>
{
    public List<string> UserNames { get; set; } = [];
}
public class GetUserReputationsResponse
{
    public Dictionary<string, string> Results { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

[Route("/search")]
public class SearchPosts : IGet, IReturn<SearchPostsResponse>
{
    public string? Q { get; set; }
    public string? View { get; set; }
    public int? Skip { get; set; }
    public int? Take { get; set; }
}
public class SearchPostsResponse
{
    public long Total { get; set; }
    public List<Post> Results { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[EnumAsInt]
public enum NotificationType
{
    Unknown = 0,
    NewComment = 1,
    NewAnswer = 2,
    QuestionMention = 3,
    AnswerMention = 4,
    CommentMention = 5,
}

public class Notification
{
    [AutoIncrement]
    public int Id { get; set; }
    
    [Index]
    public string UserName { get; set; }
    
    public NotificationType Type { get; set; }
    
    public int PostId { get; set; }
    
    public string RefId { get; set; } // Post or Answer or Comment
    
    public string Summary { get; set; } //100 chars
        
    public DateTime CreatedDate { get; set; }
    
    public bool Read { get; set; }
    
    public string? Href { get; set; }
    
    public string? Title { get; set; } //100 chars
    
    public string? RefUserName { get; set; }
}

[EnumAsInt]
public enum AchievementType
{
    Unknown = 0,
    NewAnswer = 1,
    AnswerUpVote = 2,
    AnswerDownVote = 3,
    NewQuestion = 4,
    QuestionUpVote = 5,
    QuestionDownVote = 6,
}

public class Achievement
{
    [AutoIncrement]
    public int Id { get; set; }
    
    [Index]
    public string UserName { get; set; }
    
    public AchievementType Type { get; set; }

    public int PostId { get; set; }
    
    public string RefId { get; set; }
    
    public string? RefUserName { get; set; }
    
    public int Score { get; set; }
    
    public bool Read { get; set; }
    
    public string? Href { get; set; }
    
    public string? Title { get; set; } //100 chars
    
    public DateTime CreatedDate { get; set; }
}

public enum ImportSite
{
    Unknown,
    StackOverflow,
    Discourse,
    Reddit,
    GitHubDiscussions,
}

public class ImportQuestion : IGet, IReturn<ImportQuestionResponse>
{
    public required string Url { get; set; }
    public required ImportSite Site { get; set; }
    public List<string>? Tags { get; set; }
}
public class ImportQuestionResponse
{
    public required AskQuestion Result { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}
