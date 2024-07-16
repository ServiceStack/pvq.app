using System.Collections.Concurrent;
using System.Data;
using System.Net.Http.Headers;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace MyApp.Data;

public class AppConfig
{
    public static AppConfig Instance { get; } = new();
    public string LocalBaseUrl { get; set; }
    public string PublicBaseUrl { get; set; }
    public string BaseUrl =>
#if DEBUG
        LocalBaseUrl;
#else
        PublicBaseUrl;
#endif

    public string AiServerBaseUrl { get; set; }
    public string AiServerApiKey { get; set; }
    public string? RedditClient { get; set; }
    public string? RedditSecret { get; set; }
    public string? RedditAccessToken { get; set; }

    public JsonApiClient CreateAiServerClientIgnoringSsl(string baseUrl)
    {
        var client = new JsonApiClient(AiServerBaseUrl);
            
        // Ignore local SSL Errors
        var handler = HttpUtils.HttpClientHandlerFactory();
        handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true;
        var httpClient = new HttpClient(handler, disposeHandler:client.HttpMessageHandler == null) {
            BaseAddress = new Uri(AiServerBaseUrl),
        };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AiServerApiKey);
        client = new JsonApiClient(httpClient) {
            BearerToken = AiServerApiKey
        };
        return client;
    }
    
    public JsonApiClient CreateAiServerClient()
    {
        // if (Env.IsLinux && AiServerBaseUrl.StartsWith("https://localhost"))
        //     return CreateAiServerClientIgnoringSsl(AiServerBaseUrl);
        
        return new JsonApiClient(AiServerBaseUrl) { BearerToken = AiServerApiKey };
    }

    public string CacheDir { get; set; }
    public string ProfilesDir { get; set; }
    public string NotificationsEmail { get; set; } = "notifications@pvq.app";
    public string? GitPagesBaseUrl { get; set; }
    public ConcurrentDictionary<string,int> UsersReputation { get; set; } = new();
    public ConcurrentDictionary<string,int> UsersQuestions { get; set; } = new();
    public ConcurrentDictionary<string,int> UsersUnreadAchievements { get; set; } = new();
    public ConcurrentDictionary<string,int> UsersUnreadNotifications { get; set; } = new();
    public ConcurrentDictionary<string, long> PostsLastUpdated { get; set; } = new(); // RefId => UnixTimeMs
    
    public string MasterPassword { get; set; }
    public HashSet<string> AllTags { get; set; } = [];
    public List<ApplicationUser> ModelUsers { get; set; } = [];

    public static string[] DeprecatedModels = ["deepseek-coder","gemma-2b","qwen-4b","deepseek-coder-33b"];

    public static (string Model, int Questions)[] GetActiveModelsForQuestions(int questionsCount) =>
        ModelsForQuestions.Where(x => questionsCount >= x.Questions && !DeprecatedModels.Contains(x.Model)).ToArray();

    public static (string Model, int Questions)[] GetActiveModelsForQuestionLevel(int level) =>
        ModelsForQuestions.Where(x => level == x.Questions && !DeprecatedModels.Contains(x.Model)).ToArray();

    public static (string Model, int Questions)[] ModelsForQuestions =
    [
        ("deepseek-coder", 0),
        ("gemma-2b", 0),
        ("qwen-4b", 0),
        ("deepseek-coder-33b", 100),
        
        ("phi", 0),
        ("codellama", 0),
        ("mistral", 0),
        ("llama3-8b", 0),
        ("gemma2:27b", 0),
        ("gemini-pro", 0),
        ("mixtral", 3),
        ("gemini-flash", 5),
        ("gpt3.5-turbo", 10),
        ("claude3-haiku", 25),
        ("llama3-70b", 50),
        ("qwen2-72b", 75),
        ("command-r", 100),
        ("wizardlm", 175),
        ("claude-3-5-sonnet", 250),
        ("gemini-pro-1.5", 350),
        ("command-r-plus", 450),
        ("gpt4-turbo", 600),
        ("claude3-opus", 750),
    ];

    public static int[] QuestionLevels = ModelsForQuestions.Select(x => x.Questions).Distinct().OrderBy(x => x).ToArray();

    public void LoadTags(FileInfo allTagsFile)
    {
        if (!allTagsFile.Exists)
            throw new FileNotFoundException(allTagsFile.Name);
        
        using var stream = allTagsFile.OpenRead();
        foreach (var line in stream.ReadLines())
        {
            AllTags.Add(line.Trim());
        }
    }
    
    public ApplicationUser DefaultUser { get; set; } = new()
    {
        Model = "unknown",
        UserName = "unknown",
        ProfilePath = "/img/profiles/user2.svg",
    };
    
    public Dictionary<string,string> ModelAliases = new()
    {
        ["deepseek-coder-6.7b"] = "deepseek-coder",
    };

    public ApplicationUser? GetModelUserById(string id) => ModelUsers.Find(x => x.Id == id);
    public ApplicationUser? GetModelUser(string model)
    {
        ModelAliases.TryGetValue(model, out var alias);
        var user = ModelUsers.Find(x => x.Model == model || x.UserName == model || x.UserName == alias);
        return user;
    }

    public ApplicationUser GetApplicationUser(string model) => GetModelUser(model) ?? DefaultUser;
    public string GetUserName(string model) => GetModelUser(model)?.UserName ?? model;
    
    private long nextPostId = -1;
    public void SetInitialPostId(long initialValue) => this.nextPostId = initialValue;
    public long LastPostId => Interlocked.Read(ref nextPostId);
    public long GetNextPostId() => Interlocked.Increment(ref nextPostId);

    public void SetLastUpdated(string id, DateTime lastUpdated) => PostsLastUpdated[id] = lastUpdated.ToUnixTimeMs();  
    public void SetLastUpdated(string id, long unixTimeMs) => PostsLastUpdated[id] = unixTimeMs;  
    
    public long GetLastUpdated(IDbConnection db, string id)
    {
        return PostsLastUpdated.GetOrAdd(id, key =>
        {
            var date = db.Scalar<DateTime?>(db.From<StatTotals>().Where(x => x.Id == id)
                .Select(x => x.LastUpdated));
            return date?.ToUnixTimeMs() ?? DateTimeExtensions.UnixEpoch;
        });
    }

    public string GetReputation(string? userName) => GetReputationValue(userName).ToHumanReadable();
    
    public int GetReputationValue(string? userName) => 
        userName == null || !UsersReputation.TryGetValue(userName, out var reputation) 
            ? 1 
            : reputation;

    public bool CanUseModel(string userName, string model)
    {
        if (Stats.IsAdminOrModerator(userName))
            return true;
        var questionsCount = GetQuestionCount(userName);
        
        var modelLevel = GetModelLevel(model);
        return modelLevel != -1 && questionsCount >= modelLevel;
    }

    public int GetModelLevel(string model)
    {
        foreach (var entry in ModelsForQuestions)
        {
            if (entry.Model == model)
                return entry.Questions;
        }
        return -1;
    }

    public int GetQuestionCount(string? userName) => userName switch
    {
        "stackoverflow" or "reddit" or "discourse" => 5,
        "pvq" => 25,
        "mythz" => 100,
        _ => userName == null || !UsersQuestions.TryGetValue(userName, out var count)
            ? 0
            : count + (Stats.IsAdminOrModerator(userName) ? 10 : 0)
    };

    public void Init(IDbConnection db)
    {
        ModelUsers = db.Select(db.From<ApplicationUser>().Where(x => x.Model != null
            || x.UserName == "most-voted" || x.UserName == "accepted"));
        
        ResetInitialPostId(db);

        UpdateUsersReputation(db);
        UpdateUsersQuestions(db);
        
        ResetUsersReputation(db);
        ResetUsersQuestions(db);
        
        ResetUsersUnreadAchievements(db);
        ResetUsersUnreadNotifications(db);
    }

    public void UpdateUsersReputation(IDbConnection db)
    {
        // User Reputation Score:
        // +1 point for each Question or Answer submitted
        // +10 points for each Up Vote received on Question or Answer
        // -1 point for each Down Vote received on Question or Answer
        
        db.ExecuteNonQuery(
            @"UPDATE UserInfo SET Reputation = UserScores.total
                FROM (SELECT CreatedBy, sum(score) as total FROM
                        (SELECT CreatedBy, count(*) as score FROM StatTotals WHERE CreatedBy IS NOT NULL GROUP BY 1
                         UNION
                         SELECT RefUserName, count(*) * 10 as score FROM Vote WHERE RefUserName IS NOT NULL AND Score > 0 GROUP BY 1
                         UNION 
                         SELECT RefUserName, count(*) * -1 as score FROM Vote WHERE RefUserName IS NOT NULL AND Score < 0 GROUP BY 1)
                      GROUP BY 1) as UserScores
                WHERE UserName = UserScores.CreatedBy");
    }

    public void UpdateUsersQuestions(IDbConnection db)
    {
        db.ExecuteNonQuery(@"UPDATE UserInfo SET QuestionsCount = UserQuestions.total
            FROM (select createdBy, count(*) as total FROM post WHERE CreatedBy IS NOT NULL GROUP BY 1) as UserQuestions
          WHERE UserName = UserQuestions.CreatedBy");
    }

    public void ResetInitialPostId(IDbConnection db)
    {
        var maxPostId = db.Scalar<int>("SELECT MAX(Id) FROM Post");
        SetInitialPostId(Math.Max(100_000_000, maxPostId + 1));
    }

    public void ResetUsersReputation(IDbConnection db)
    {
        UsersReputation = new(db.Dictionary<string, int>(db.From<UserInfo>()
            .Select(x => new { x.UserName, x.Reputation })));
    }

    public void ResetUsersQuestions(IDbConnection db)
    {
        UsersQuestions = new(db.Dictionary<string, int>(db.From<UserInfo>()
            .Select(x => new { x.UserName, x.QuestionsCount })));
    }

    public void ResetUsersUnreadNotifications(IDbConnection db)
    {
        UsersUnreadNotifications = new(db.Dictionary<string, int>(
            "SELECT UserName, Count(*) AS Total FROM Notification WHERE Read = false GROUP BY UserName HAVING COUNT(*) > 0"));
    }

    public async Task ResetUnreadNotificationsForAsync(IDbConnection db, string userName)
    {
        UsersUnreadNotifications[userName] =
            (int)await db.CountAsync(db.From<Notification>().Where(x => x.UserName == userName && x.Read == false));
    }

    public void ResetUsersUnreadAchievements(IDbConnection db)
    {
        UsersUnreadAchievements = new(db.Dictionary<string, int>(
            "SELECT UserName, Count(*) AS Total FROM Achievement WHERE Read = false GROUP BY UserName HAVING COUNT(*) > 0"));
    }

    public async Task ResetUserQuestionsAsync(IDbConnection db, string userName)
    {
        var questionsCount = (int)await db.CountAsync<Post>(x => x.CreatedBy == userName);
        UsersQuestions[userName] = questionsCount;
        await db.UpdateOnlyAsync(() => 
            new UserInfo { QuestionsCount = questionsCount }, x => x.UserName == userName);
    }
    
    public List<string> GetAnswerModelUsersFor(string? userName)
    {
        var questionsCount = GetQuestionCount(userName);
        
        var models = GetActiveModelsForQuestions(questionsCount)
            .Select(x => x.Model)
            .ToList();
        
        // Remove lower quality models
        if (models.Contains("gemma"))
            models.RemoveAll(x => x == "gemma:2b");
        if (models.Contains("mixtral"))
            models.RemoveAll(x => x == "mistral");
        if (models.Contains("deepseek-coder:33b"))
            models.RemoveAll(x => x == "deepseek-coder:6.7b");
        if (models.Contains("gpt-4-turbo"))
            models.RemoveAll(x => x == "gpt3.5-turbo");
        if (models.Contains("command-r-plus"))
            models.RemoveAll(x => x == "command-r");
        if (models.Contains("claude-3-opus"))
            models.RemoveAll(x => x is "claude-3-haiku" or "claude-3-sonnet");
        if (models.Contains("claude-3-sonnet"))
            models.RemoveAll(x => x is "claude-3-haiku");
        if (models.Contains("gemini-pro-1.5"))
            models.RemoveAll(x => x is "gemini-flash" or "gemini-pro");
        if (models.Contains("gemini-flash"))
            models.RemoveAll(x => x is "gemini-pro");
        return models;
    }

    public List<string> GetAnswerModelsFor(string? userName)
    {
        return GetAnswerModelUsersFor(userName)
            .Map(x => GetModelUser(x)?.Model ?? throw HttpError.NotFound("Model User not found: " + x));
    }

    public void IncrUnreadNotificationsFor(string userName)
    {
        UsersUnreadNotifications.AddOrUpdate(userName, 1, (_, count) => count + 1);
    }

    public void IncrUnreadAchievementsFor(string userName)
    {
        UsersUnreadAchievements.AddOrUpdate(userName, 1, (_, count) => count + 1);
    }

    public bool HasUnreadNotifications(string? userName)
    {
        return userName != null && UsersUnreadNotifications.TryGetValue(userName, out var count) && count > 0;
    }

    public bool HasUnreadAchievements(string? userName)
    {
        return userName != null && UsersUnreadAchievements.TryGetValue(userName, out var count) && count > 0;
    }

    public bool IsHuman(string? userName) => userName != null && GetModelUser(userName) == null;
}
