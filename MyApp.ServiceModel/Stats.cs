using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

public static class Stats
{
    public static bool IsAdminOrModerator(string? userName) => 
        userName is "admin" or "mythz" or "layoric";
}

public static class Databases
{
    // Keep heavy writes of stats + analytics in separate DB
    public const string Analytics = nameof(Analytics);
    public const string Search = nameof(Search);
}

public class StatTotals
{
    public required string Id { get; set; } // PostId or PostId-UserName (Answer)
    public int PostId { get; set; }
    public int FavoriteCount { get; set; }
    public int ViewCount { get; set; }
    public int UpVotes { get; set; }
    public int DownVotes { get; set; }
    public int StartingUpVotes { get; set; }
    public DateTime ModifiedDate { get; set; }
}

[NamedConnection(Databases.Analytics)]
public class StatBase
{
    public string RefId { get; set; }
    public string? UserName { get; set; }
    public string? RemoteIp { get; set; }
    public DateTime CreatedDate { get; set; }
}

[Icon(Svg = Icons.Stats)]
public class PostView : StatBase
{
    [AutoIncrement]
    public int Id { get; set; }
    public int PostId { get; set; }
}

[Icon(Svg = Icons.Stats)]
public class SearchView : StatBase
{
    [AutoIncrement]
    public int Id { get; set; }
    public string? Query { get; set; }
}

[Tag(Tag.Tasks)]
[ExcludeMetadata]
[Restrict(InternalOnly = true)]
public class AnalyticsTasks
{
    public SearchView? RecordSearchStat { get; set; }
    public PostView? RecordPostStat { get; set; }
}

