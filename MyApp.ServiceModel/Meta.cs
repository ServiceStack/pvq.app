namespace MyApp.ServiceModel;

public class Meta
{
    // Question + Answer Stats Totals
    public List<StatTotals> StatTotals { get; set; }
    public Dictionary<string, int> ModelVotes { get; set; }
    public List<Comment> Comments { get; set; } = [];
}
