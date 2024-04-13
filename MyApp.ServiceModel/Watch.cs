using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[UniqueConstraint(nameof(UserName), nameof(PostId))]
public class WatchPost
{
    [AutoIncrement]
    public int Id { get; set; }
    public string UserName { get; set; }
    public int PostId { get; set; }
    public DateTime CreatedDate { get; }
    public DateTime? AfterDate { get; } // Email new answers 1hr after asking question
}
    
[UniqueConstraint(nameof(UserName), nameof(Tag))]
public class WatchTag
{
    [AutoIncrement]
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Tag { get; set; }
    public DateTime CreatedDate { get; }
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
    public DateTime? AfterDate { get; } // Email new answers 1hr after receiving them
    public int? MailMessageId { get; set; }
}
