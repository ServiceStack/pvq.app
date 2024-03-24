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

/// <summary>
/// Aggregate Stats for Questions(Id=PostId) and Answers(Id=PostId-UserName)
/// </summary>
public class StatTotals
{
    // PostId (Question) or PostId-UserName (Answer)
    public required string Id { get; set; }
    
    [Index]
    public int PostId { get; set; }
    
    public int FavoriteCount { get; set; }
    
    // post.ViewCount + Sum(PostView.PostId)
    public int ViewCount { get; set; }
    
    // Sum(Vote(PostId).Score > 0) 
    public int UpVotes { get; set; }
    
    // Sum(Vote(PostId).Score < 0) 
    public int DownVotes { get; set; }
    
    // post.Score || Meta.ModelVotes[PostId] (Model Ranking Score)
    public int StartingUpVotes { get; set; }

    public int GetScore() => StartingUpVotes + UpVotes - DownVotes;

    private sealed class StatTotalsEqualityComparer : IEqualityComparer<StatTotals>
    {
        public bool Equals(StatTotals x, StatTotals y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Id == y.Id && x.PostId == y.PostId && x.FavoriteCount == y.FavoriteCount && x.ViewCount == y.ViewCount && x.UpVotes == y.UpVotes && x.DownVotes == y.DownVotes && x.StartingUpVotes == y.StartingUpVotes;
        }

        public int GetHashCode(StatTotals obj)
        {
            return HashCode.Combine(obj.Id, obj.PostId, obj.FavoriteCount, obj.ViewCount, obj.UpVotes, obj.DownVotes, obj.StartingUpVotes);
        }
    }

    public static IEqualityComparer<StatTotals> StatTotalsComparer { get; } = new StatTotalsEqualityComparer();

    public bool Matches(StatTotals? other)
    {
        return other == null || UpVotes != other.UpVotes || DownVotes != other.DownVotes || StartingUpVotes != other.DownVotes;
    }
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
    [Index]
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
    public SearchView? RecordSearchView { get; set; }
    public PostView? RecordPostView { get; set; }
}

public class StartJob
{
    public int Id { get; set; }
    public string? Worker { get; set; }
    public string? WorkerIp { get; set; }
}

public class DbWrites
{
    public Vote? RecordPostVote { get; set; }
    public Post? CreatePost { get; set; }
    public List<PostJob>? CreatePostJobs { get; set; }
    public StartJob? StartJob { get; set; }
    public int? AnswerAddedToPost { get; set; }
    public List<int>? CompleteJobIds { get; set; }
}