namespace MyApp.ServiceModel;

// AnswerId = `{PostId}-{UserName}`
// RefId = PostId | AnswerId
public class Meta
{
    // PostId
    public int Id { get; set; }

    // ModelName => Votes
    public Dictionary<string, int> ModelVotes { get; set; } = [];

    // ModelName => Vote Reason
    public Dictionary<string, string> ModelReasons { get; set; } = [];

    // RefId => Comments
    public Dictionary<string, List<Comment>> Comments { get; set; } = [];

    // Question + Answer Stats Totals
    public List<StatTotals> StatTotals { get; set; } = [];

    public DateTime ModifiedDate { get; set; }
}
