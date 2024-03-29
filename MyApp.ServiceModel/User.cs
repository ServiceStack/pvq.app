using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[Tag(Tag.User)]
[ValidateIsAuthenticated]
public class UpdateUserProfile : IPost, IReturn<UpdateUserProfileResponse>
{
}

public class UpdateUserProfileResponse
{
    public ResponseStatus ResponseStatus { get; set; }
}

[Route("/avatar/{UserName}", "GET")]
public class GetUserAvatar : IGet, IReturn<byte[]>
{
    public string UserName { get; set; }
}

public class UserInfo
{
    [PrimaryKey]
    public string UserId { get; set; }
    [Index(Unique = true)]
    public string UserName { get; set; }
    [Default(1)]
    public int Reputation { get; set; }
    [Default(0)]
    public int QuestionsCount { get; set; }
    [Default(0)]
    public int EditQuestionsCount { get; set; }
    [Default(0)]
    public int AnswersCount { get; set; }
    [Default(0)]
    public int EditAnswersCount { get; set; }
    [Default(0)]
    public int UpVotesCount { get; set; }
    [Default(0)]
    public int DownVotesCount { get; set; }
    [Default(0)]
    public int CommentsCount { get; set; }
    [Default(0)]
    public int EditCommentsCount { get; set; }
    [Default(0)]
    public int ReportsCount { get; set; }
    [Default(0)]
    public int ReportsReceived { get; set; } // Questions, Answers & Comments with Reports
    public DateTime? LastActivityDate { get; set; }
}
