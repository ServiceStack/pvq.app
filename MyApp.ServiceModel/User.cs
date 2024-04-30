using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[Tag(Tag.User)]
[ValidateIsAuthenticated]
public class UpdateUserProfile : IPost, IReturn<UpdateUserProfileResponse> {}

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
    public bool HasUnread { get; set; }
    public List<Notification> Results { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

[ValidateIsAuthenticated]
public class GetLatestAchievements : IGet, IReturn<GetLatestAchievementsResponse> {}
public class GetLatestAchievementsResponse
{
    public bool HasUnread { get; set; }
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

[Route("/q/{RefId}")]
[Route("/q/{RefId}/{UserId}")]
public class ShareContent : IGet, IReturn<string>
{
    [ValidateNotEmpty]
    public string RefId { get; set; }
    public int? UserId { get; set; }
}

public enum FlagType
{
    Unknown = 0,
    Spam = 1,
    Offensive = 2,
    Duplicate = 3,
    NotRelevant = 4,
    LowQuality = 5,
    Plagiarized = 6,
    NeedsReview = 7,
}

[ValidateIsAuthenticated]
public class FlagContent : IPost, IReturn<EmptyResponse>
{
    [ValidateNotEmpty]
    public string RefId { get; set; }
    public FlagType Type { get; set; }
    public string? Reason { get; set; }
}

public class Flag
{
    [AutoIncrement]
    public int Id { get; set; }
    public string RefId { get; set; }
    public int PostId { get; set; }
    public FlagType Type { get; set; }
    public string? Reason { get; set; }
    public string? UserName { get; set; }
    public string? RemoteIp { get; set; }
    public DateTime CreatedDate { get; set; }
}

[ValidateIsAuthenticated]
[ValidateHasRole(Roles.Admin)]
public class AdminResetCommonPassword
{
}

public class AdminResetCommonPasswordResponse
{
    public List<string> UpdatedUsers { get; set; }
}
