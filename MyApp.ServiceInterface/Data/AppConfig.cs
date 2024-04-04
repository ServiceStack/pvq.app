using System.Collections.Concurrent;
using System.Data;
using MyApp.ServiceModel;
using ServiceStack.OrmLite;

namespace MyApp.Data;

public class AppConfig
{
    public static AppConfig Instance { get; } = new();
    public string LocalBaseUrl { get; set; }
    public string PublicBaseUrl { get; set; }
    public string CacheDir { get; set; }
    public string ProfilesDir { get; set; }
    public string? GitPagesBaseUrl { get; set; }
    public ConcurrentDictionary<string,int> UsersReputation { get; set; } = new();
    public ConcurrentDictionary<string,int> UsersQuestions { get; set; } = new();
    public HashSet<string> AllTags { get; set; } = [];
    public List<ApplicationUser> ModelUsers { get; set; } = [];

    public static (string Model, int Questions)[] ModelsForQuestions =
    [
#if DEBUG
        ("phi", 0),
        ("gemma:2b", 0),
        ("gemma", 3),
        ("mixtral", 5),
        ("gemini-pro", 10),
        ("claude-3-haiku", 25),
        ("deepseek-coder:33b", 50),
        ("claude-3-sonnet", 75),
        ("gpt-4-turbo", 100),
        ("claude-3-opus", 200),
#else
        ("phi", 0),
        ("gemma:2b", 0),
        ("qwen:4b", 0),
        ("codellama", 0),
        ("deepseek-coder:6.7b", 0),
        ("mistral", 0),
        ("gemma", 3),
        ("mixtral", 5),
        // ("gemini-pro", 10),
        // ("claude-3-haiku", 25),
        // ("deepseek-coder:33b", 50),
        // ("claude-3-sonnet", 75),
        // ("gpt-4-turbo", 100),
        // ("claude-3-opus", 200),
#endif
    ];

    public static int[] QuestionLevels = ModelsForQuestions.Select(x => x.Questions).Distinct().OrderBy(x => x).ToArray();
    
    public ApplicationUser DefaultUser { get; set; } = new()
    {
        Model = "unknown",
        UserName = "unknown",
        ProfilePath = "/img/profiles/user2.svg",
    };

    public ApplicationUser GetApplicationUser(string model)
    {
        var user = ModelUsers.FirstOrDefault(x => x.Model == model || x.UserName == model);
        return user ?? DefaultUser;
    }
    
    public ApplicationUser? GetModelUser(string model)
    {
        var user = ModelUsers.Find(x => x.Model == model || x.UserName == model);
        return user;
    }
    
    public string GetUserName(string model)
    {
        var user = ModelUsers.FirstOrDefault(x => x.Model == model || x.UserName == model);
        return user?.UserName ?? model;
    }
    
    private long nextPostId = -1;
    public void SetInitialPostId(long initialValue) => this.nextPostId = initialValue;
    public long LastPostId => Interlocked.Read(ref nextPostId);
    public long GetNextPostId() => Interlocked.Increment(ref nextPostId);
    
    public int GetReputation(string? userName) => 
        userName == null || !UsersReputation.TryGetValue(userName, out var reputation) 
            ? 1 
            : reputation;

    public int GetQuestionCount(string? userName) => 
        userName == null || !UsersQuestions.TryGetValue(userName, out var count) 
            ? 0 
            : count;

    public void Init(IDbConnection db)
    {
        ModelUsers = db.Select(db.From<ApplicationUser>().Where(x => x.Model != null
            || x.UserName == "most-voted" || x.UserName == "accepted"));
        
        ResetInitialPostId(db);

        UpdateUsersReputation(db);
        UpdateUsersQuestions(db);
        
        ResetUsersReputation(db);
        ResetUsersQuestions(db);
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

    public async Task ResetUserQuestionsAsync(IDbConnection db, string userName)
    {
        var questionsCount = (int)await db.CountAsync<Post>(x => x.CreatedBy == userName);
        UsersQuestions[userName] = questionsCount;
        await db.UpdateOnlyAsync(() => 
            new UserInfo { QuestionsCount = questionsCount }, x => x.UserName == userName);
    }
    
    public List<string> GetAnswerModelsFor(string? userName)
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

}
