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

        public string UserName { get; set; }
    
        /// <summary>
        /// 1 for UpVote, -1 for DownVote
        /// </summary>
        public int Score { get; set; }
    }
    
    public class StatTotals
    {
        // PostId (Question) or PostId-UserName (Answer)
        public required string Id { get; set; }
    
        [Index]
        public int PostId { get; set; }
    
        public int FavoriteCount { get; set; }
    
        // post.ViewCount + Sum(PostView.PostId)
        public int ViewCount { get; set; }
    
        // post.Score + Sum(Vote(PostId).Score > 0) 
        public int UpVotes { get; set; }
    
        // Sum(Vote(PostId).Score < 0) 
        public int DownVotes { get; set; }
    
        // Model Ranking Score Meta.ModelVotes[PostId]
        public int StartingUpVotes { get; set; }
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

    public override void Up()
    {
        Db.CreateTable<UserInfo>();
        Db.CreateTable<Vote>();
        Db.CreateTable<Job>();
        Db.CreateTable<StatTotals>();
        Db.CreateTable<PostJob>();
        
        Db.ExecuteSql("INSERT INTO UserInfo (UserId, UserName) SELECT Id, UserName FROM AspNetUsers");
    }

    public override void Down()
    {
        Db.DeleteAll<UserInfo>();
        
        Db.DropTable<PostJob>();
        Db.DropTable<StatTotals>();
        Db.DropTable<Job>();
        Db.DropTable<Vote>();
        Db.DropTable<UserInfo>();
    }
}
