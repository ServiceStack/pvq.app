using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace MyApp.Migrations;

public class Migration1000 : MigrationBase
{
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

        /// <summary>
        /// User who voted
        /// </summary>
        public string UserName { get; set; }
    
        /// <summary>
        /// 1 for UpVote, -1 for DownVote
        /// </summary>
        public int Score { get; set; }
    
        /// <summary>
        /// User who's Post (Q or A) was voted on
        /// </summary>
        public string? RefUserName { get; set; }
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
    
    public class PostJob
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int PostId { get; set; }
        public string Model { get; set; }
        public string Title { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? StartedDate { get; set; }
        public string? Worker { get; set; }
        public string? WorkerIp { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? Error { get; set; }
        public int RetryCount { get; set; }
    }
    
    public class UserInfo
    {
        [PrimaryKey]
        public string UserId { get; set; }
        [Index(Unique = true)]
        public string UserName { get; set; }
        [Default(1)]
        public int Reputation { get; set; }
        [Default(0)]
        public int QuestionsCount { get; set; }
        [Default(0)]
        public int EditQuestionsCount { get; set; }
        [Default(0)]
        public int AnswersCount { get; set; }
        [Default(0)]
        public int EditAnswersCount { get; set; }
        [Default(0)]
        public int UpVotesCount { get; set; }
        [Default(0)]
        public int DownVotesCount { get; set; }
        [Default(0)]
        public int CommentsCount { get; set; }
        [Default(0)]
        public int EditCommentsCount { get; set; }
        [Default(0)]
        public int ReportsCount { get; set; }
        [Default(0)]
        public int ReportsReceived { get; set; } // Questions, Answers & Comments with Reports
        public DateTime? LastActivityDate { get; set; }
    }

    [EnumAsInt]
    public enum NotificationType {}

    public class Notification
    {
        [AutoIncrement]
        public int Id { get; set; }
    
        [Index]
        public string UserName { get; set; }
    
        public NotificationType Type { get; set; }
    
        public int PostId { get; set; }
    
        public string RefId { get; set; }
    
        public string PostTitle { get; set; }
        
        public string Summary { get; set; }
    
        public string Href { get; set; }
    
        public DateTime CreatedDate { get; set; }
    
        public bool Read { get; set; }
    }
    
    [EnumAsInt]
    public enum AchievementType {}

    public class Achievement
    {
        [AutoIncrement]
        public int Id { get; set; }
    
        [Index]
        public string UserName { get; set; }
    
        public AchievementType Type { get; set; }

        public int PostId { get; set; }
    
        public string RefId { get; set; }
    
        public string? RefUserName { get; set; }
    
        public int Score { get; set; }
    
        public bool Read { get; set; }
    
        public DateTime CreatedDate { get; set; }
    }

    public override void Up()
    {
        Db.CreateTable<Achievement>();
        Db.CreateTable<Notification>();
        Db.CreateTable<UserInfo>();
        Db.CreateTable<Vote>();
        Db.CreateTable<Job>();
        Db.CreateTable<PostJob>();
        
        Db.ExecuteSql("INSERT INTO UserInfo (UserId, UserName) SELECT Id, UserName FROM AspNetUsers");
        Db.ExecuteSql("UPDATE StatTotals SET CreatedBy = substr(Id,instr(Id,'-')+1) WHERE instr(Id,'-') > 0 AND CreatedBy IS NULL");
    }

    public override void Down()
    {
        Db.DeleteAll<UserInfo>();
        
        Db.DropTable<PostJob>();
        Db.DropTable<Job>();
        Db.DropTable<Vote>();
        Db.DropTable<UserInfo>();
        Db.DropTable<Notification>();
        Db.DropTable<Achievement>();
    }
}
