using MyApp.ServiceModel;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.Logging;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace MyApp.Tests;

[TestFixture, Explicit, Category(nameof(MigrationTasks))]
public class DbTasks
{
    IDbConnectionFactory ResolveDbFactory() => new ConfigureDb().ConfigureAndResolve<IDbConnectionFactory>();
    public string GetHostDir()
    {
        LogManager.LogFactory = new ConsoleLogFactory();
        var appSettings = JSON.parse(File.ReadAllText(Path.GetFullPath("appsettings.json")));
        return appSettings.ToObjectDictionary()["HostDir"].ToString()!;
    }
    
    [Test]
    public void Generate_Tags()
    {
        var hostDir = GetHostDir();
        var tagsPath = Path.GetFullPath(Path.Combine(hostDir, "App_Data", "tags.txt"));
        
        using var db = ResolveDbFactory().OpenDbConnection();
        var tags = db.Column<string>(db.From<Post>().SelectDistinct(x => x.Tags));
        
        var allTags = new HashSet<string>();
        foreach (var tag in tags)
        {
            if (tag == null) continue;
            foreach (var t in tag.Split(','))
            {
                allTags.Add(t.Trim('[',']').Trim());
            }
        }

        var writer = new StreamWriter(tagsPath, append:false);
        var i = 0;
        foreach (var tag in allTags.OrderBy(x => x))
        {
            if (i++ > 1000) 
                writer.Flush();
            writer.WriteLine(tag);
        }
        writer.Flush();
    }
}