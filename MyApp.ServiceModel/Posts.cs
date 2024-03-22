// Complete declarative AutoQuery services for Bookings CRUD example:
// https://docs.servicestack.net/autoquery-crud-bookings

using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[Icon(Svg = Icons.Post)]
[Description("StackOverflow Question")]
[Notes("A StackOverflow Question Post")]
public class Post
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

    [Ignore] public string? Body { get; set; }
}

public class PostJob
{
    public int JobId { get; set; }
    public int PostId { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public List<string> Tags { get; set; }
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

public class CreateAnswer : IReturnVoid
{
    public int PostId { get; set; }
    public string Model { get; set; }
    public string Body { get; set; }
    public string UserName { get; set; }
    public int? JobId { get; set; }
    public string? WorkerId { get; set; }
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

public class Choice
{
    public int Index { get; set; }
    public ChoiceMessage Message { get; set; }
}

public class ChoiceMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
}

public class Answer
{
    public string Id { get; set; }
    public string Object { get; set; }
    public long Created { get; set; }
    public string Model { get; set; }
    public List<Choice> Choices { get; set; }
    public Dictionary<string, int> Usage { get; set; }
    public decimal Temperature { get; set; }
    public List<Comment> Comments { get; set; } = [];
    public int Votes => UpVotes - DownVotes;
    public int UpVotes { get; set; }
    public int DownVotes { get; set; }
}

public class Comment
{
    public string Body { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class QuestionAndAnswers
{
    public int Id => Post.Id;
    public Post Post { get; set; }

    public List<Comment> PostComments { get; set; } = [];
    public List<Answer> Answers { get; set; } = [];
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