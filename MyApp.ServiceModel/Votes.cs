using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[UniqueConstraint(nameof(UserId), nameof(AnswerId))]
public class Vote
{
    [AutoIncrement]
    public int Id { get; set; }
        
    public int UserId { get; set; }
        
    public int PostId { get; set; }
        
    [Required]
    public string AnswerId { get; set; }

    public int Score { get; set; }
}
