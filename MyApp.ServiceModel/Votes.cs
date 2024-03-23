using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[UniqueConstraint(nameof(RefId), nameof(UserName))]
public class Vote
{
    [AutoIncrement]
    public int Id { get; set; }
        
    [Index]
    public int PostId { get; set; }
        
    /// <summary>
    /// `Post.Id` or `${Post.Id}-{UserName}` (Answer)
    /// </summary>
    [Required]
    public string RefId { get; set; }

    public string UserName { get; set; }
    
    /// <summary>
    /// 1 for UpVote, -1 for DownVote
    /// </summary>
    public int Score { get; set; }
}
