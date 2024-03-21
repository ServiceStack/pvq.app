using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace MyApp.Migrations;

public class Migration1000 : MigrationBase
{
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
    
    public override void Up()
    {
        Db.CreateTable<Vote>();
        Db.CreateTable<Job>();
    }

    public override void Down()
    {
        Db.DropTable<Vote>();
        Db.DropTable<Job>();
    }
}
