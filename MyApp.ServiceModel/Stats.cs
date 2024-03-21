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

[NamedConnection(Databases.Analytics)]
public class StatBase
{
    public string RefId { get; set; }
    public string? UserName { get; set; }
    public string? RemoteIp { get; set; }
    public DateTime CreatedDate { get; set; }
}

[Icon(Svg = Icons.Stats)]
public class PostStat : StatBase
{
    [AutoIncrement]
    public int Id { get; set; }
    public int PostId { get; set; }
}

[Icon(Svg = Icons.Stats)]
public class SearchStat : StatBase
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
    public SearchStat? RecordSearchStat { get; set; }
    public PostStat? RecordPostStat { get; set; }
}

