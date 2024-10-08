using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[UniqueConstraint(nameof(UserName), nameof(PostId))]
public class WatchPost
{
    [AutoIncrement]
    public int Id { get; set; }
    public string UserName { get; set; }
    public int PostId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? AfterDate { get; set; } // Email new answers 1hr after asking question
}

[UniqueConstraint(nameof(Date), nameof(Tag))]
public class WatchPostMail
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Date { get; set; }
    public string Tag { get; set; }
    public List<string> UserNames { get; set; }
    public List<int> PostIds { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime GeneratedDate { get; set; }
    public int? MailRunId { get; set; }
}

[UniqueConstraint(nameof(UserName), nameof(Tag))]
public class WatchTag
{
    [AutoIncrement]
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Tag { get; set; }
    public DateTime CreatedDate { get; set; }
}
    
public enum PostEmailType
{
    NewAnswer,
}
    
public class PostEmail
{
    [AutoIncrement]
    public int Id { get; set; }
    public PostEmailType Type { get; set; }
    public int PostId { get; set; }
    public string? RefId { get; set; }
    public string Email { get; set; }
    public string? UserName { get; set; }        
    public string? DisplayName { get; set; }
    public DateTime? AfterDate { get; set; } // Email new answers 1hr after receiving them
    public int? MailMessageId { get; set; }
}

[ValidateIsAuthenticated]
public class WatchContent : IPost, IReturn<EmptyResponse>
{
    public int? PostId { get; set; }
    public string? Tag { get; set; }
}
[ValidateIsAuthenticated]
public class UnwatchContent : IPost, IReturn<EmptyResponse>
{
    public int? PostId { get; set; }
    public string? Tag { get; set; }
}

[ValidateIsAuthenticated]
public class WatchStatus : IGet, IReturn<BoolResponse>
{
    public int? PostId { get; set; }
    public string? Tag { get; set; }
}

[ValidateIsAuthenticated]
public class WatchTags : IPost, IReturn<EmptyResponse>
{
    public List<string>? Subscribe { get; set; }
    public List<string>? Unsubscribe { get; set; }
}

[ValidateIsAuthenticated]
public class GetWatchedTags : IGet, IReturn<GetWatchedTagsResponse> {}
public class GetWatchedTagsResponse
{
    public List<string> Results { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

