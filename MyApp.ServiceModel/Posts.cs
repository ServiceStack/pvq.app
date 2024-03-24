﻿// Complete declarative AutoQuery services for Bookings CRUD example:
// https://docs.servicestack.net/autoquery-crud-bookings

using System.ComponentModel.DataAnnotations;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[Icon(Svg = Icons.Post)]
[Description("StackOverflow Question")]
[Notes("A StackOverflow Question Post")]
public class Post
{
    public int Id { get; set; }

    [ServiceStack.DataAnnotations.Required] public int PostTypeId { get; set; }

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
    [Ignore] public string? CreatedBy { get; set; }
    [Ignore] public string? ModifiedBy { get; set; }
    [Ignore] public string? RefId { get; set; }
}

public class PostJob
{
    [AutoIncrement]
    public int Id { get; set; }
    public int PostId { get; set; }
    public string Model { get; set; }
    public string Title { get; set; }
    public string? Body { get; set; }
    public List<string> Tags { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? StartedDate { get; set; }
    public string? Worker { get; set; }
    public string? WorkerIp { get; set; }
    public DateTime? CompletedDate { get; set; }
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
public class RestoreModelQueues : IGet, IReturn<StringsResponse> {}

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
}

public class Comment
{
    public string Body { get; set; }
    public string CreatedBy { get; set; }
    public int? UpVotes { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class QuestionAndAnswers
{
    public int Id => Post.Id;
    public Post Post { get; set; }
    public Meta? Meta { get; set; }
    public List<Answer> Answers { get; set; } = [];

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

[ValidateHasRole(Roles.Moderator)]
public class CreateWorkerAnswer : IReturn<IdResponse>
{
    public int PostId { get; set; }
    [ValidateNotEmpty]
    public string Model { get; set; }
    [ValidateNotEmpty]
    public string Json { get; set; }
    public int? PostJobId { get; set; }
}

[ValidateIsAuthenticated]
public class AskQuestion : IPost, IReturn<AskQuestionResponse>
{
    [ValidateNotEmpty, ValidateMinimumLength(20), ValidateMaximumLength(120)]
    [Input(Type = "text", Help = "A summary of what your main question is asking"), FieldCss(Field="col-span-12")]
    public required string Title { get; set; }
    
    [ValidateNotEmpty, ValidateMinimumLength(30), ValidateMaximumLength(32768)]
    [Input(Type="MarkdownInput", Help = "Include all information required for someone to identity and resolve your exact question"), FieldCss(Field="col-span-12", Input="h-56")]
    public required string Body { get; set; }
    
    [ValidateNotEmpty, ValidateMinimumLength(2, Message = "At least 1 tag required"), ValidateMaximumLength(120)]
    [Input(Type = "tag", Help = "Up to 5 tags relevant to your question"), FieldCss(Field="col-span-12")]
    public required List<string> Tags { get; set; }
    
    [Input(Type="hidden")]
    public string? RefId { get; set; }
}

public class AskQuestionResponse
{
    public int Id { get; set; }
    public string Slug { get; set; }
    public string? RedirectTo { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

public class PreviewMarkdown : IPost, IReturn<string>
{
    public string Markdown { get; set; }
}
