using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace MyApp.Migrations;

[NamedConnection(MyApp.ServiceModel.Databases.Analytics)]
public class Migration1001 : MigrationBase
{
    public class StatTotals
    {
        public required string Id { get; set; } // PostId or PostId-UserName (Answer)
        public int PostId { get; set; }
        public int FavoriteCount { get; set; }
        public int ViewCount { get; set; }
        public int UpVotes { get; set; }
        public int DownVotes { get; set; }
        public int StartingUpVotes { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

    public class StatBase
    {
        public string RefId { get; set; }
        public string? UserName { get; set; }
        public string RemoteIp { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class PostView : StatBase
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int PostId { get; set; }
    }

    public class SearchView : StatBase
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string? Query { get; set; }
    }

    public override void Up()
    {
        Db.CreateTable<StatTotals>();
        Db.CreateTable<PostView>();
        Db.CreateTable<SearchView>();
    }

    public override void Down()
    {
        Db.CreateTable<StatTotals>();
        Db.CreateTable<PostView>();
        Db.CreateTable<SearchView>();
    }
}
