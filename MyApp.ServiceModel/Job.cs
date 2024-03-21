using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

public class Job
{
    [AutoIncrement]
    public int Id { get; set; }
        
    public int PostId { get; set; }

    public string Model { get; set; }
        
    public DateTime CreatedDate { get; set; }
        
    public DateTime? StartedDate { get; set; }
        
    public string? WorkerId { get; set; }

    public string? WorkerIp { get; set; }
        
    public DateTime? CompletedDate { get; set; }
        
    public string? Response { get; set; }
}
