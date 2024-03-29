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
    public HashSet<string> AllTags { get; set; } = [];
    public List<ApplicationUser> ModelUsers { get; set; } = [];
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

    public void Init(IDbConnection db)
    {
        ModelUsers = db.Select(db.From<ApplicationUser>().Where(x => x.Model != null
            || x.UserName == "most-voted" || x.UserName == "accepted"));
        
        ResetInitialPostId(db);

        UpdateUsersReputation(db);
        ResetUsersReputation(db);
    }

    public void UpdateUsersReputation(IDbConnection db)
    {
        db.ExecuteNonQueryAsync(@"update UserInfo set Reputation = UserScores.total
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

    public void ResetUsersReputation(IDbConnection db)
    {
        UsersReputation = new(db.Dictionary<string, int>(db.From<UserInfo>()
            .Select(x => new { x.UserName, x.Reputation })));
    }

    public void ResetInitialPostId(IDbConnection db)
    {
        var maxPostId = db.Scalar<int>("SELECT MAX(Id) FROM Post");
        SetInitialPostId(Math.Max(100_000_000, maxPostId));
    }
}
