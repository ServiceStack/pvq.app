using System.Runtime.Serialization;
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

public class CreateAvatar : IGet, IReturn<string>
{
    [ValidateNotEmpty]
    public string UserName { get; set; }
    public string? TextColor { get; set; }
    public string? BgColor { get; set; }
}

[ValidateIsAuthenticated]
public class GetLatestNotifications : IGet, IReturn<GetLatestNotificationsResponse> {}
public class GetLatestNotificationsResponse
{
    public List<Notification> Results { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

[ValidateIsAuthenticated]
public class GetLatestAchievements : IGet, IReturn<GetLatestAchievementsResponse> {}
public class GetLatestAchievementsResponse
{
    public List<Achievement> Results { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

[ValidateIsAuthenticated]
public class MarkAsRead : IPost, IReturn<EmptyResponse>
{
    [IgnoreDataMember]
    public string UserName { get; set; }
    public List<int>? NotificationIds { get; set; }
    public bool? AllNotifications { get; set; }
    public List<int>? AchievementIds { get; set; }
    public bool? AllAchievements { get; set; }
}

[ExcludeMetadata]
[ValidateIsAdmin]
public class GetUsersInfo : IGet, IReturn<GetUsersInfoResponse>
{
}

public class GetUsersInfoResponse
{
    public Dictionary<string,int> UsersReputation { get; set; } = new();
    public Dictionary<string,int> UsersQuestions { get; set; } = new();
    public Dictionary<string,int> UsersUnreadAchievements { get; set; } = new();
    public Dictionary<string,int> UsersUnreadNotifications { get; set; } = new();
    public ResponseStatus? ResponseStatus { get; set; }
}