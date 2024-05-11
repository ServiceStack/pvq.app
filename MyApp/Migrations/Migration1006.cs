using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace MyApp.Migrations;

public class Migration1006 : MigrationBase
{
    [UniqueConstraint(nameof(Date), nameof(Tag))]
    public class WatchPostMail
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Date { get; set; }
        public string Tag { get; set; }
        public List<string> UserNames { get; set; }
        public List<int> PostIds { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public int? MailRunId { get; set; }
    }
    
    public override void Up()
    {
        Db.CreateTable<WatchPostMail>();
    }

    public override void Down()
    {
        Db.DropTable<WatchPostMail>();
    }
}
