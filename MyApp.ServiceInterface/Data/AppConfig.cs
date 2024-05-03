using System.Collections.Concurrent;
using System.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

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
    public JsonApiClient CreateAiServerClient() => new(AiServerBaseUrl) { BearerToken = AiServerApiKey };
    
    public string CacheDir { get; set; }
    public string ProfilesDir { get; set; }
    public string NotificationsEmail { get; set; } = "notifications@pvq.app";
    public string? GitPagesBaseUrl { get; set; }
    public ConcurrentDictionary<string,int> UsersReputation { get; set; } = new();
    public ConcurrentDictionary<string,int> UsersQuestions { get; set; } = new();
    public ConcurrentDictionary<string,int> UsersUnreadAchievements { get; set; } = new();
    public ConcurrentDictionary<string,int> UsersUnreadNotifications { get; set; } = new();
    
    public string MasterPassword { get; set; }
    public HashSet<string> AllTags { get; set; } = [];
    public List<ApplicationUser> ModelUsers { get; set; } = [];

    public static (string Model, int Questions)[] ModelsForQuestions =
    [
        ("phi", 0),
        ("codellama", 0),
        ("mistral", 0),
        ("gemma", 0),
        ("llama3-8b", 0),
        ("gemini-pro", 3),
        ("mixtral", 5),
        ("gpt3.5-turbo", 10),
        ("claude3-haiku", 25),
        ("command-r", 50),
        ("wizardlm", 100),
        ("claude3-sonnet", 175),
        ("command-r-plus", 150),
        ("gpt4-turbo", 250),
        ("claude3-opus", 400),
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

    public string GetReputation(string? userName) => GetReputationValue(userName).ToHumanReadable();
    
    public int GetReputationValue(string? userName) => 
        userName == null || !UsersReputation.TryGetValue(userName, out var reputation) 
            ? 1 
            : reputation;

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
        SetInitialPostId(Math.Max(100_000_000, maxPostId));
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
        
        var models = ModelsForQuestions.Where(x => x.Questions <= questionsCount)
            .Select(x => x.Model)
            .ToList();
        if (models.Contains("gemma"))
            models.RemoveAll(x => x == "gemma:2b");
        if (models.Contains("deepseek-coder:33b"))
            models.RemoveAll(x => x == "deepseek-coder:6.7b");
        if (models.Contains("claude-3-opus"))
            models.RemoveAll(x => x is "claude-3-haiku" or "claude-3-sonnet");
        if (models.Contains("claude-3-sonnet"))
            models.RemoveAll(x => x is "claude-3-haiku");
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
