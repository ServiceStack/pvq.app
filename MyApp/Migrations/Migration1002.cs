using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace MyApp.Migrations;

public class Migration1002 : MigrationBase
{
    [UniqueConstraint(nameof(UserName), nameof(PostId))]
    public class WatchPost
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string UserName { get; set; }
        public int PostId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? AfterDate { get; set; } // Email new answers 1hr after asking question
    }
    
    [UniqueConstraint(nameof(UserName), nameof(Tag))]
    public class WatchTag
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Tag { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    
    public enum PostEmailType
    {
        NewAnswer,
    }
    
    public class PostEmail
    {
        [AutoIncrement]
        public int Id { get; set; }
        public PostEmailType Type { get; set; }
        public int PostId { get; set; }
        public string? RefId { get; set; }
        public string Email { get; set; }
        public string? UserName { get; set; }        
        public string? DisplayName { get; set; }
        public DateTime? AfterDate { get; set; } // Email new answers 1hr after receiving them
        public int? MailMessageId { get; set; }
    }

    public override void Up()
    {
        Db.CreateTable<WatchPost>();
        Db.CreateTable<WatchTag>();
        Db.CreateTable<PostEmail>();
    }

    public override void Down()
    {
        Db.DropTable<PostEmail>();
        Db.DropTable<WatchTag>();
        Db.DropTable<WatchPost>();
    }
}