﻿using System.Collections.Concurrent;
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
    public string CacheDir { get; set; }
    public string ProfilesDir { get; set; }
    public string NotificationsEmail { get; set; } = "notifications@pvq.app";
    public string? GitPagesBaseUrl { get; set; }
    public ConcurrentDictionary<string,int> UsersReputation { get; set; } = new();
    public ConcurrentDictionary<string,int> UsersQuestions { get; set; } = new();
    public ConcurrentDictionary<string,int> UsersUnreadAchievements { get; set; } = new();
    public ConcurrentDictionary<string,int> UsersUnreadNotifications { get; set; } = new();
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
        //("deepseek-coder:33b", 50),
        ("claude-3-sonnet", 75),
        ("gpt-4-turbo", 100),
        ("claude-3-opus", 200),
#else
        ("phi", 0),                 // demis,macbook
        ("gemma:2b", 0),            // demis,macbook
        ("qwen:4b", 0),             // demis,darren
        ("codellama", 0),           // demis,darren
        ("deepseek-coder:6.7b", 0), // demis
        ("mistral", 0),             // demis
        ("gemma", 3),               // demis
        ("mixtral", 5),             // darren
        ("gemini-pro", 10),         // hetzner,macbook
        ("claude-3-haiku", 25),     // hetzner,macbook
        //("deepseek-coder:33b", 50),
        ("claude-3-sonnet", 75),    // hetzner,macbook
        ("gpt-4-turbo", 100),       // hetzner,macbook
        ("claude-3-opus", 200),     // hetzner,macbook
        
        //hetzner: model-worker.mjs gemini-pro,claude-3-haiku,claude-3-sonnet,gpt-4-turbo,claude-3-opus 
        //macbook: model-worker.mjs phi,gemma:2b,gemini-pro,claude-3-haiku,claude-3-sonnet,gpt-4-turbo,claude-3-opus
        //demis:   model-worker.mjs phi,gemma:2b,qwen:4b,codellama,deepseek-coder:6.7b,mistral,gemma
        //darren1: model-worker.mjs qwen:4b,codellama
        //darren2: model-worker.mjs mixtral
#endif
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
    
    public ApplicationUser? GetModelUser(string model)
    {
        string? alias;
        ModelAliases.TryGetValue(model, out alias);
        
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
