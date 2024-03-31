using System.Collections.Concurrent;
using System.Data;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
        ("mixtral", 10),
        ("gemini-pro", 20),
        ("claude-3-sonnet-20240229", 50),
        ("gpt-4-turbo-preview", 50),
        ("claude-3-opus-20240229", 100),
#else
        ("phi", 0),
        ("gemma:2b", 0),
        ("qwen:4b", 0),
        ("codellama", 0),
        ("deepseek-coder:6.7b", 0),
        ("mistral", 0),
        ("gemma", 3),
        ("mixtral", 10),
        // ("gemini-pro", 20),
        // ("claude-3-sonnet-20240229", 50),
        // ("gpt-4-turbo-preview", 50),
        // ("claude-3-opus-20240229", 100),
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
        db.ExecuteNonQuery(@"update UserInfo set Reputation = UserScores.total
            from (select createdBy, sum(count) as total from
                (select createdBy, count(*) as count from post where CreatedBy is not null group by 1
                union
                select userName, count(*) as count from vote group by 1
                union
                select substring(id,instr(id,'-')+1) as userName, sum(UpVotes) as count from StatTotals where instr(id,'-') group by 1
                union
                select substring(id,instr(id,'-')+1) as userName, sum(DownVotes) as count from StatTotals where instr(id,'-') group by 1)
                group by 1) as UserScores
          where UserName = UserScores.CreatedBy");
    }

    public void UpdateUsersQuestions(IDbConnection db)
    {
        db.ExecuteNonQuery(@"update UserInfo set QuestionsCount = UserQuestions.total
            from (select createdBy, count(*) as total from post where CreatedBy is not null group by 1) as UserQuestions
          where UserName = UserQuestions.CreatedBy");
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
        return models;
    }

}
