using System.Text;
using System.Text.RegularExpressions;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace MyApp.ServiceInterface.Clients;

public class DiscourseClient
{
    public static ILog Log = LogManager.GetLogger(typeof(DiscourseClient));

    public string ApiKey { get; private set; }
    public string UserName { get; private set; }
    public string Password { get; private set; }
    private string csrf { get; set; }
    private readonly JsonApiClient client;

    public DiscourseClient(string baseUrl, string apiKey, string userName, string password)
    {
        ApiKey = apiKey;
        UserName = userName;
        Password = password;
        client = new JsonApiClient(baseUrl);
        client.AddHeader("Api-Key", ApiKey);
        client.AddHeader("Api-Username", UserName);
    }

    public string CreateApiUrl(string apiPath) => client.BaseUri.CombineWith(apiPath);

    public string GetTopJson()
    {
        var url = CreateApiUrl("top.json");
        var json = url.GetJsonFromUrl();
        return json;
    }

    public List<DiscourseUser> GetDiscourseUnapprovedUsers()
    {
        using (JsConfig.With(new Config {
                   PropertyConvention = PropertyConvention.Lenient,
                   TextCase = TextCase.SnakeCase,
               }))
        {
            var url = CreateApiUrl("/admin/users/list/blocked.json")
                .AddQueryParam("show_emails","true")
                .AddQueryParam("order","created");
            var json = GetJsonFromUrl(url);
            var results = json.FromJson<DiscourseUser[]>();
            var unapprovedUsers = results.Where(x => x.Approved == false).ToList();
            return unapprovedUsers;
        }
    }

    public DiscourseUser GetDiscourseUserByFilter(string filter)
    {
        using (JsConfig.With(new Config {
            PropertyConvention = PropertyConvention.Lenient,
            TextCase = TextCase.SnakeCase,
        }))
        {
            var url = CreateApiUrl("/admin/users/list/all.json").AddQueryParam("filter", filter);
            var json = GetJsonFromUrl(url);
            var results = json.FromJson<DiscourseUser[]>();
            if (results?.Length != 1)
                throw new Exception($"Expected 1 result for 'filter={filter}', but was {results?.Length}");

            return results[0];
        }
    }
    
    public void LoginAdmin()
    {
        Login(UserName, Password);
    }

    [Route("/session")]
    public class LoginAuth
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    [Route("/session/csrf")]
    public class GetCsrfToken { }
    public class GetCsrfTokenResponse
    {
        public string csrf { get; set; }
    }

    public string PostToUrl(string url, object formData) => SendFormDataToUrl(url, method:HttpMethods.Post, formData);
    public string PutToUrl(string url, object formData) => SendFormDataToUrl(url, method:HttpMethods.Put, formData);

    private string SendFormDataToUrl(string url, string method, object formData)
    {
        return client.HttpClient!.SendStringToUrl(url: url, method: method,
            contentType: MimeTypes.FormUrlEncoded + MimeTypes.Utf8Suffix,
            requestBody: formData != null ? QueryStringSerializer.SerializeToString(formData) : null,
            requestFilter: req => req.With(c =>
            {
                c.AddHeader("Api-Key", ApiKey);
                c.AddHeader("Api-Username", UserName);
                
                if (csrf != null)
                    c.AddHeader("X-CSRF-Token", csrf);
                c.AddHeader("X-Request-With", "XMLHttpRequest");
            }));
    }

    public string GetJsonFromUrl(string url)
    {
        return client.HttpClient!.SendStringToUrl(url: url, method: HttpMethods.Get,
            requestFilter: req => req.With(c =>
            {
                c.AddHeader("Api-Key", ApiKey);
                c.AddHeader("Api-Username", UserName);
                
                if (csrf != null)
                    c.AddHeader("X-CSRF-Token", csrf);
                c.AddHeader("X-Request-With", "XMLHttpRequest");
            }));
    }

    /// <summary>
    /// All the admin related tasks are not available via just an API key and user, this mimicks logging in a normal user
    /// If the user is admin, admin methods are available.
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="pass"></param>
    public void Login(string userName, string pass)
    {
        //client.Post <LoginAuthResponse>("/session", new LoginAuth { username = userName, password = pass });
        var csrfWebResponse = client.Get<GetCsrfTokenResponse>(new GetCsrfToken());
        client.Headers!.Add("X-CSRF-Token", csrfWebResponse.csrf);
        csrf = csrfWebResponse.csrf;
        client.Headers.Add("X-Request-With", "XMLHttpRequest");
        client.SetCredentials(userName, pass);
        var url = client.BaseUri.CombineWith("/session");

        Log.Info($"Login(): Sending POST Request to {url} ...");

        var formData = new LoginAuth { username = userName, password = pass };
        var response = client.HttpClient!.SendStringToUrl(url: url, method: HttpMethods.Post,
            contentType: MimeTypes.FormUrlEncoded + MimeTypes.Utf8Suffix,
            requestBody: QueryStringSerializer.SerializeToString(formData),
            requestFilter: req => req.With(c =>
            {
                if (csrf != null)
                    c.AddHeader("X-CSRF-Token", csrf);
                c.AddHeader("X-Request-With", "XMLHttpRequest");
            }));
    }

    private static JsConfigScope JsonScope()
    {
        return JsConfig.With(new Config
        {
            PropertyConvention = PropertyConvention.Lenient,
            TextCase = TextCase.SnakeCase
        });
    }

    public GetCategoriesResponse GetCategories()
    {
        using (JsonScope())
        {
            var request = new GetCategories();
            return client.Get(request);
        }
    }

    public GetCategoryResponse GetCategory(int id)
    {
        using (JsonScope())
        {
            var request = new GetCategory { Id = id };
            return client.Get(request);
        }
    }

    public GetLatestTopicsResponse GetTopics()
    {
        using (JsonScope())
        {
            var request = new GetLatestTopics();
            return client.Get(request);
        }
    }

    public IEnumerable<DiscourseUser> AdminGetUsers()
    {
        using (JsonScope())
        {
            var page = 1;
            while (true)
            {
                var request = new AdminGetUsersWithEmail { page = page++ };
                var requestUrl = request.ToGetUrl()
                    .AddQueryParam("show_emails", "true");
                requestUrl = client.BaseUri.Substring(0, client.BaseUri.Length - 1) + requestUrl;
                var res = GetJsonFromUrl(requestUrl);
                
                var users = JsonSerializer.DeserializeFromString<List<DiscourseUser>>(res);
                if (users.Count > 0)
                {
                    foreach (var user in users)
                    {
                        yield return user;
                    }
                }
                else break;
            }
        }
    }

    public GetUserByIdResponse GetUserById(string userId)
    {
        using (JsonScope())
        {
            var request = new GetUserById { UserId = userId };
            var requestUrl = request.ToGetUrl();
            requestUrl = client.BaseUri.Substring(0, client.BaseUri.Length - 1) + requestUrl;
            var res = GetJsonFromUrl(requestUrl);
            return JsonSerializer.DeserializeFromString<GetUserByIdResponse>(res);
        }
    }

    public GetUserEmailByIdResponse GetUserEmail(string userId)
    {
        using (JsonScope())
        {
            var request = new AdminGetUserEmailById { UserId = userId };
            var requestUrl = request.ToGetUrl();
            requestUrl = client.BaseUri.Substring(0, client.BaseUri.Length - 1) + requestUrl;
            var res = GetJsonFromUrl(requestUrl);
            return JsonSerializer.DeserializeFromString<GetUserEmailByIdResponse>(res);
        }
    }

    public List<DiscourseUser> AdminFindUsersByFilter(string filter)
    {
        using (JsonScope())
        {
            var request = new AdminGetUsers();
            var requestUrl = request.ToGetUrl()
                .AddQueryParam("filter", filter).AddQueryParam("show_emails", "true");
            requestUrl = client.BaseUri.Substring(0, client.BaseUri.Length - 1) + requestUrl;
            var res = GetJsonFromUrl(requestUrl);
            return JsonSerializer.DeserializeFromString<List<DiscourseUser>>(res);
        }
    }

    public GetUserEmailByIdResponse AdminGetUserEmailById(string userId)
    {
        using (JsonScope())
        {
            var request = new AdminGetUserEmailById { UserId = userId };
            var requestUrl = request.ToGetUrl();
            requestUrl = client.BaseUri.Substring(0, client.BaseUri.Length - 1) + requestUrl;
            var res = GetJsonFromUrl(requestUrl);
            return JsonSerializer.DeserializeFromString<GetUserEmailByIdResponse>(res);
        }
    }

    public GetTopicResponse GetTopic(int id)
    {
        using (JsonScope())
        {
            var request = new GetTopic { TopicId = id };
            return client.Get(request);
        }
    }
}

public class DiscoursePostDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string AvatarTemplate { get; set; }
    public string CreatedAt { get; set; }
    public string Cooked { get; set; }
    public int PostNumber { get; set; }
    public int PostType { get; set; }
    public string UpdatedAt { get; set; }
    public int ReplyCount { get; set; }
    public string ReplyToPostNumber { get; set; }
    public int QuoteCount { get; set; }
    public int IncomingLinkCount { get; set; }
    public int Reads { get; set; }
    public int ReadersCount { get; set; }
    public int Score { get; set; }
    public bool Yours { get; set; }
    public int TopicId { get; set; }
    public string TopicSlug { get; set; }
    public string PrimaryGroupName { get; set; }
    public string FlairName { get; set; }
    public string FlairUrl { get; set; }
    public string FlairBgColor { get; set; }
    public string FlairColor { get; set; }
    public string FlairGroupId { get; set; }
    public int Version { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanRecover { get; set; }
    public bool CanSeeHiddenPost { get; set; }
    public bool CanWiki { get; set; }
    public string UserTitle { get; set; }
    public bool Bookmarked { get; set; }
    public string Raw { get; set; }
    public List<ActionSummaryDto> ActionsSummary { get; set; }
    public bool Moderator { get; set; }
    public bool Admin { get; set; }
    public bool Staff { get; set; }
    public int UserId { get; set; }
    public bool Hidden { get; set; }
    public int TrustLevel { get; set; }
    public string DeletedAt { get; set; }
    public bool UserDeleted { get; set; }
    public string EditReason { get; set; }
    public bool CanViewEditHistory { get; set; }
    public bool Wiki { get; set; }
    public string ReviewableId { get; set; }
    public int ReviewableScoreCount { get; set; }
    public int ReviewableScorePendingCount { get; set; }
    public List<string> MentionedUsers { get; set; }
    public string Name { get; set; }
    public string DisplayUsername { get; set; }
}

public class ActionSummaryDto
{
    public int Id { get; set; }
    public int Count { get; set; }
    public bool Acted { get; set; }
    public bool CanUndo { get; set; }
    public bool CanAct { get; set; }
}

public class GetUserByIdResponse
{
    public DiscourseUser User { get; set; }
}



[Route("/categories.json", "GET")]
public class GetCategories : IReturn<GetCategoriesResponse>
{

}

[Route("/c/{Id}", "GET")]
public class GetCategory : IReturn<GetCategoryResponse>
{
    public int Id { get; set; }
}

[Route("/latest.json", "GET")]
public class GetLatestTopics : IReturn<GetLatestTopicsResponse>
{

}

[Route("/t/{TopicId}", "GET")]
public class GetTopic : IReturn<GetTopicResponse>
{
    public int TopicId { get; set; }
}

[ExcludeMetadata]
[Route("/admin/users.json", "GET")]
public class AdminGetUsersWithEmail : IReturn<List<DiscourseUser>>
{
    public int page { get; set; }
}

[ExcludeMetadata]
[Route("/admin/users")]
public class AdminGetUsers : IReturn<List<DiscourseUser>>
{
}

[ExcludeMetadata]
[Route("/users/{UserId}/emails.json")]
public class AdminGetUserEmailById : IReturn<GetUserEmailByIdResponse>
{
    public string UserId { get; set; }
}

public class GetUserEmailByIdResponse
{
    public string Email { get; set; }
    public string AssociatedAccounts { get; set; }
}

[Route("/users/{UserId}")]
public class GetUserById : IReturn<DiscourseUser>
{
    public string UserId { get; set; }
}

public static class Extensions
{
    /// <summary>
    /// From http://stackoverflow.com/a/2921135/670151
    /// </summary>
    /// <param name="phrase"></param>
    /// <returns></returns>
    public static string GenerateSlug(this string phrase)
    {
        string str = phrase.RemoveAccent().ToLower()
            .Replace("#", "sharp")  // c#, f# => csharp, fsharp
            .Replace("+", "p");      // c++ => cpp

        // invalid chars           
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "-");
        // convert multiple spaces into one space   
        str = Regex.Replace(str, @"\s+", " ").Trim();
        // cut and trim 
        str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
        str = Regex.Replace(str, @"\s", "-"); // hyphens   
        return str;
    }

    public static string RemoveAccent(this string txt)
    {
        byte[] bytes = Encoding.GetEncoding("Cyrillic").GetBytes(txt);
        return Encoding.ASCII.GetString(bytes);
    }
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public int UploadedAvatarId { get; set; }
    public string AvatarTemplate { get; set; }
}

public class DiscourseUser
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string AvatarTemplate { get; set; }
    public string Name { get; set; }
    public string CreatedAt { get; set; }
    public string Email { get; set; }
    public bool CanEdit { get; set; }
    public bool CanEditUsername { get; set; }
    public bool CanEditEmail { get; set; }
    public bool CanEditName { get; set; }
    public bool CanSendPrivateMessages { get; set; }
    public bool CanSendPrivateMessageToUser { get; set; }
    public string BioExcerpt { get; set; }
    public int TrustLevel { get; set; }
    public bool Moderator { get; set; }
    public bool Admin { get; set; }
    public int BadgeCount { get; set; }
    public int NotificationCount { get; set; }
    public bool HasTitleBadges { get; set; }
    public int PostCount { get; set; }
    public bool CanBeDeleted { get; set; }
    public bool CanDeleteAllPosts { get; set; }
    public bool EmailDigests { get; set; }
    public bool EmailPrivateMessages { get; set; }
    public bool EmailDirect { get; set; }
    public bool EmailAlways { get; set; }
    public int DigestAfterDays { get; set; }
    public bool MailingListMode { get; set; }
    public int AutoTrackTopicsAfterMsecs { get; set; }
    public int NewTopicDurationMinutes { get; set; }
    public bool ExternalLinksInNewTab { get; set; }
    public bool DynamicFavicon { get; set; }
    public bool EnableQuoting { get; set; }
    public bool DisableJumpReply { get; set; }
    public bool Approved { get; set; }

    public bool IsSuspended =>
        !string.IsNullOrEmpty(SuspendedTill);


    public string SuspendedAt { get; set; }
    public string SuspendedTill { get; set; }
}

public class Poster
{
    public int Id { get; set; }
    public string Username { get; set; }
    public int UploadedAvatarId { get; set; }
    public string AvatarTemplate { get; set; }
    public int? PostCount { get; set; }
}

public class Topic
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string FancyTitle { get; set; }
    public string Slug { get; set; }
    public int PostsCount { get; set; }
    public int ReplyCount { get; set; }
    public int HighestPostNumber { get; set; }
    public string ImageUrl { get; set; }
    public string CreatedAt { get; set; }
    public string LastPostedAt { get; set; }
    public bool Bumped { get; set; }
    public string BumpedAt { get; set; }
    public bool Unseen { get; set; }
    public bool Pinned { get; set; }

    //public object Unpinned { get; set; }
    public bool Visible { get; set; }
    public bool Closed { get; set; }
    public bool Archived { get; set; }
    public bool? Bookmarked { get; set; }
    public bool? Liked { get; set; }
    public Poster LastPoster { get; set; }
    public string Excerpt { get; set; }
    public int? LastReadPostNumber { get; set; }
    public int? Unread { get; set; }
    public int? NewPosts { get; set; }
    public int? NotificationLevel { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }
    public string TextColor { get; set; }
    public string Slug { get; set; }
    public int TopicCount { get; set; }
    public int PostCount { get; set; }
    public string Description { get; set; }
    public string DescriptionText { get; set; }
    public string TopicUrl { get; set; }
    public bool ReadRestricted { get; set; }

    //public object Permission { get; set; }
    //public object NotificationLevel { get; set; }
    //public object LogoUrl { get; set; }
    //public object BackgroundUrl { get; set; }
    public bool CanEdit { get; set; }
    public int TopicsDay { get; set; }
    public int TopicsWeek { get; set; }
    public int TopicsMonth { get; set; }
    public int TopicsYear { get; set; }
    public int PostsDay { get; set; }
    public int PostsWeek { get; set; }
    public int PostsMonth { get; set; }
    public int PostsYear { get; set; }
    public string DescriptionExcerpt { get; set; }
    public List<int> FeaturedUserIds { get; set; }
    public List<Topic> Topics { get; set; }
    public bool? IsUncategorized { get; set; }

    public List<GroupPermission> GroupPermissions { get; set; }
}

public class TopicDetails
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string FancyTitle { get; set; }
    public string Slug { get; set; }
    public int PostsCount { get; set; }
    public int ReplyCount { get; set; }
    public int HighestPostNumber { get; set; }

    public string ImageUrl { get; set; }
    public string CreatedAt { get; set; }
    public string LastPostedAt { get; set; }
    public bool Bumped { get; set; }
    public string BumpedAt { get; set; }
    public bool Unseen { get; set; }
    public bool Pinned { get; set; }


    //public object Unpinned { get; set; }
    public string Excerpt { get; set; }
    public bool Visible { get; set; }
    public bool Closed { get; set; }
    public bool Archived { get; set; }

    //public object Bookmarked { get; set; }
    //public object Liked { get; set; }
    public int Views { get; set; }
    public int LikeCount { get; set; }
    public bool HasSummary { get; set; }
    public string Archetype { get; set; }
    public string LastPosterUsername { get; set; }
    public int CategoryId { get; set; }
    public bool PinnedGlobally { get; set; }
    public List<Poster> Posters { get; set; }
}

public class ActionsSummary
{
    public int Id { get; set; }
    public int Count { get; set; }
    public bool Hidden { get; set; }
    public bool CanAct { get; set; }
    public bool CanDeferFlags { get; set; }
}

public class Post
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Username { get; set; }
    public string AvatarTemplate { get; set; }
    public int UploadedAvatarId { get; set; }
    public string CreatedAt { get; set; }
    public string Cooked { get; set; }
    public int PostNumber { get; set; }
    public int PostType { get; set; }
    public string UpdatedAt { get; set; }
    public int LikeCount { get; set; }
    public int ReplyCount { get; set; }
    public int? ReplyToPostNumber { get; set; }
    public int QuoteCount { get; set; }

    //public object AvgTime { get; set; }
    public int IncomingLinkCount { get; set; }
    public int Reads { get; set; }
    public double Score { get; set; }
    public bool Yours { get; set; }
    public int TopicId { get; set; }
    public string TopicSlug { get; set; }
    public object TopicAutoCloseAt { get; set; }
    public string DisplayUsername { get; set; }

    public string PrimaryGroupName { get; set; }
    public int Version { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanRecover { get; set; }
    public bool Read { get; set; }
    public string UserTitle { get; set; }
    public List<ActionsSummary> ActionsSummary { get; set; }
    public bool Moderator { get; set; }
    public bool Admin { get; set; }
    public bool Staff { get; set; }
    public int UserId { get; set; }
    public bool Hidden { get; set; }
    public int? HiddenReasonId { get; set; }
    public int TrustLevel { get; set; }

    //public object DeletedAt { get; set; }
    public bool UserDeleted { get; set; }
    public string EditReason { get; set; }
    public bool CanViewEditHistory { get; set; }
    public bool Wiki { get; set; }
}

public class PostStream
{
    public List<Post> Posts { get; set; }
    public List<int> Stream { get; set; }
}

public class TopicMetaData
{
    public object AutoCloseAt { get; set; }
    public object AutoCloseHours { get; set; }
    public bool AutoCloseBasedOnLastPost { get; set; }
    public User CreatedBy { get; set; }
    public Poster LastPoster { get; set; }
    public List<Poster> Participants { get; set; }
    public List<Topic> SuggestedTopics { get; set; }
    public int NotificationLevel { get; set; }
    public bool CanMovePosts { get; set; }
    public bool CanEdit { get; set; }
    public bool CanRecover { get; set; }
    public bool CanRemoveAllowedUsers { get; set; }
    public bool CanCreatePost { get; set; }
    public bool CanReplyAsNewTopic { get; set; }
    public bool CanFlagTopic { get; set; }
}

public class CategoryList
{
    public bool CanCreateCategory { get; set; }
    public bool CanCreateTopic { get; set; }

    //public object Draft { get; set; }
    public string DraftKey { get; set; }
    public int DraftSequence { get; set; }
    public List<Category> Categories { get; set; }
}

public class TopicList
{
    public bool CanCreateTopic { get; set; }

    //public object draft { get; set; }
    public string DraftKey { get; set; }
    public int DraftSequence { get; set; }
    public int PerPage { get; set; }
    public List<TopicDetails> Topics { get; set; }
}

public class GroupPermission
{
    public int PermissionType { get; set; }
    public string GroupName { get; set; }
}

public class GetCategoriesResponse
{
    public List<User> FeaturedUsers { get; set; }
    public CategoryList CategoryList { get; set; }
}

public class GetCategoryResponse
{
    public List<User> Users { get; set; }
    public TopicList TopicList { get; set; }
}

public class GetLatestTopicsResponse
{
    public List<User> Users { get; set; }
    public TopicList TopicList { get; set; }
}

public class GetTopicsResponse
{
    public List<User> Users { get; set; }
    public TopicList TopicList { get; set; }
}

public class GetTopicResponse
{
    public PostStream PostStream { get; set; }
    public int Id { get; set; }
    public string Title { get; set; }
    public string FancyTitle { get; set; }
    public int PostsCount { get; set; }
    public string CreatedAt { get; set; }
    public int Views { get; set; }
    public int ReplyCount { get; set; }
    public int ParticipantCount { get; set; }
    public int LikeCount { get; set; }
    public string LastPostedAt { get; set; }
    public bool Visible { get; set; }
    public bool Closed { get; set; }
    public bool Archived { get; set; }
    public bool HasSummary { get; set; }
    public string Archetype { get; set; }
    public string Slug { get; set; }
    public int CategoryId { get; set; }
    public int WordCount { get; set; }

    //public object DeletedAt { get; set; }
    //public object Draft { get; set; }

    public string DraftKey { get; set; }
    public int DraftSequence { get; set; }

    //public bool? Unpinned { get; set; }
    public bool PinnedGlobally { get; set; }
    public bool Pinned { get; set; }

    //public object PinnedAt { get; set; }
    public TopicMetaData Details { get; set; }
    public int HighestPostNumber { get; set; }

    //public object DeletedBy { get; set; }
    public bool HasDeleted { get; set; }
    public List<ActionsSummary> ActionsSummary { get; set; }
    public int ChunkSize { get; set; }
    public bool? Bookmarked { get; set; }
}