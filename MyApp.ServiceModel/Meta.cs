namespace MyApp.ServiceModel;

// AnswerId = `{PostId}-{UserName}`
// RefId = PostId | AnswerId
public class Meta
{
    // PostId
    public int Id { get; set; }

    // Model (UserName) => Votes
    public Dictionary<string, int> ModelVotes { get; set; } = [];

    // Model (UserName) => Vote Reason
    public Dictionary<string, string> ModelReasons { get; set; } = [];

    // "gradedBy": { "mixtral": ["1000-mistral","1000-gemma",..] }
    public Dictionary<string, Dictionary<string, List<string>>> GradedBy { get; set; } = [];

    // RefId => Comments
    public Dictionary<string, List<Comment>> Comments { get; set; } = [];

    // Question + Answer Stats Totals
    public List<StatTotals> StatTotals { get; set; } = [];

    public DateTime ModifiedDate { get; set; }
}
