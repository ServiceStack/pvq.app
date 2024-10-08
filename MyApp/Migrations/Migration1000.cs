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
    
    public class StatTotals
    {
        // PostId (Question) or PostId-UserName (Answer)
        public string Id { get; set; }
    
        [Index]
        public int PostId { get; set; }
    
        [Index]
        public string? CreatedBy { get; set; }
    
        public int FavoriteCount { get; set; }
    
        // post.ViewCount + Sum(PostView.PostId)
        public int ViewCount { get; set; }
    
        // Sum(Vote(PostId).Score > 0) 
        public int UpVotes { get; set; }
    
        // Sum(Vote(PostId).Score < 0) 
        public int DownVotes { get; set; }
    
        // post.Score || Meta.ModelVotes[PostId] (Model Ranking Score)
        public int StartingUpVotes { get; set; }
    
        public DateTime? LastUpdated { get; set; }
    }
    
    public class Post : IMeta
    {
        public int Id { get; set; }

        [Required] public int PostTypeId { get; set; }

        public int? AcceptedAnswerId { get; set; }

        public int? ParentId { get; set; }

        public int Score { get; set; }

        public int? ViewCount { get; set; }

        public string Title { get; set; }

        public int? FavoriteCount { get; set; }

        public DateTime CreationDate { get; set; }

        public DateTime LastActivityDate { get; set; }

        public DateTime? LastEditDate { get; set; }

        public int? LastEditorUserId { get; set; }

        public int? OwnerUserId { get; set; }

        public List<string> Tags { get; set; }

        public string Slug { get; set; }

        public string Summary { get; set; }
    
        public DateTime? RankDate { get; set; }
    
        public int? AnswerCount { get; set; }

        public string? CreatedBy { get; set; }
    
        public string? ModifiedBy { get; set; }
    
        public string? Body { get; set; }

        public string? ModifiedReason { get; set; }
    
        public DateTime? LockedDate { get; set; }

        public string? LockedReason { get; set; }
    
        public string? RefId { get; set; }

        public string? RefUrn { get; set; }

        public Dictionary<string, string>? Meta { get; set; }

        public string GetRefId() => RefId ?? (PostTypeId == 1 ? $"{Id}" : $"{Id}-{CreatedBy}");
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
    
        public string RefId { get; set; } // Post or Answer or Comment
    
        public string Summary { get; set; } //100 chars
    
        public string? Href { get; set; }
    
        public string? Title { get; set; } //100 chars
        
        public DateTime CreatedDate { get; set; }
    
        public bool Read { get; set; }
    
        public string? RefUserName { get; set; }
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
    
        public string? Href { get; set; }
    
        public string? Title { get; set; } //100 chars
    
        public DateTime CreatedDate { get; set; }
    }
    
    public enum FlagType {}
    public class Flag
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string RefId { get; set; }
        public int PostId { get; set; }
        public FlagType Type { get; set; }
        public string? Reason { get; set; }
        public string UserName { get; set; }
        public string? RemoteIp { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    public override void Up()
    {
        Db.CreateTable<Achievement>();
        Db.CreateTable<Notification>();
        Db.CreateTable<UserInfo>();
        Db.CreateTable<Vote>();
        Db.CreateTable<Job>();
        Db.CreateTable<Flag>();
        Db.CreateTableIfNotExists<StatTotals>();
        Db.CreateTableIfNotExists<Post>();
        
        Db.ExecuteSql("INSERT INTO UserInfo (UserId, UserName) SELECT Id, UserName FROM AspNetUsers");
        Db.ExecuteSql("UPDATE StatTotals SET CreatedBy = substr(Id,instr(Id,'-')+1) WHERE instr(Id,'-') > 0 AND CreatedBy IS NULL");
    }

    public override void Down()
    {
        Db.DeleteAll<UserInfo>();
        
        Db.DropTable<Job>();
        Db.DropTable<Vote>();
        Db.DropTable<UserInfo>();
        Db.DropTable<Notification>();
        Db.DropTable<Achievement>();
        Db.DropTable<Flag>();
    }
}
