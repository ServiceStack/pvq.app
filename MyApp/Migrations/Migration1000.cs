using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace MyApp.Migrations;

public class Migration1000 : MigrationBase
{
    public class Post
    {
        public int Id { get; set; }
        
        [Required]
        public int PostTypeId { get; set; }

        public int? AcceptedAnswerId { get; set; }

        public int? ParentId { get; set; }

        public int Score { get; set; }

        public int? ViewCount { get; set; }

        public string Title { get; set; }

        public string ContentLicense { get; set; }

        public int? FavoriteCount { get; set; }

        public DateTime CreationDate { get; set; }

        public DateTime LastActivityDate { get; set; }

        public DateTime? LastEditDate { get; set; }

        public int? LastEditorUserId { get; set; }

        public int? OwnerUserId { get; set; }

        public List<string> Tags { get; set; }
        
        public string Slug { get; set; }
    
        public string Summary { get; set; }
    }
    
    public override void Up()
    {
        // Db.CreateTable<Post>();
    }

    public override void Down()
    {
        // Db.DropTable<Post>();
    }
}
