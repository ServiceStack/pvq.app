using MyApp.Data;
using MyApp.ServiceInterface.App;
using MyApp.ServiceModel;
using NUnit.Framework;
using ServiceStack.Text;

namespace MyApp.Tests;

public class ImportTests
{
    AppConfig appConfig;
    
    private static AppConfig CreateAppConfig()
    {
        var hostDir = TestUtils.GetHostDir();
        var allTagsFile = new FileInfo(Path.GetFullPath(Path.Combine(hostDir, "wwwroot", "data", "tags.txt")));
        
        var to = new AppConfig();
        to.LoadTags(allTagsFile);
        return to;
    }

    public ImportTests()
    {
        appConfig = CreateAppConfig();
    }

    [Test]
    public async Task Can_import_from_discourse_url()
    {
        var command = new ImportQuestionCommand(appConfig);

        await command.ExecuteAsync(new ImportQuestion
        {
            Site = ImportSite.Discourse,
            Url = "https://forums.servicestack.net/t/postgres-array-types-as-params-for-function/8408",
        });

        var result = command.Result!;
        result.Tags.PrintDump();
        Assert.That(result.Title, Is.EqualTo("Postgres Array Types as params for function"));
        Assert.That(result.Body, Does.StartWith("Not really sure how to approach array params"));
        Assert.That(result.Tags.Count, Is.GreaterThan(0));
        Assert.That(result.RefId, Is.EqualTo("forums.servicestack.net:8408"));
    }

    [Test]
    public async Task Can_import_from_discourse_answer_url()
    {
        var command = new ImportQuestionCommand(appConfig);

        await command.ExecuteAsync(new ImportQuestion
        {
            Site = ImportSite.Discourse,
            Url = "https://forums.servicestack.net/t/postgres-array-types-as-params-for-function/8408/3?u=mythz",
        });

        var result = command.Result!;
        result.Tags.PrintDump();
        Assert.That(result.Title, Is.EqualTo("Postgres Array Types as params for function"));
        Assert.That(result.Body, Does.StartWith("Not really sure how to approach array params"));
        Assert.That(result.Tags, Is.EquivalentTo(new[] { "c#", "function", "int", "sql", "using" }));
        Assert.That(result.RefId, Is.EqualTo("forums.servicestack.net:8408"));
    }

    [Test]
    public void Can_parse_StackOverflow_html()
    {
        var html = File.ReadAllText("App_Data/stackoverflow-import.html");
        var result = ImportQuestionCommand.CreateFromStackOverflowInlineEdit(html)!;
        Assert.That(result.Title, Is.EqualTo("ServiceStack - As passthru to another ServiceStack service"));
        Assert.That(result.Body, Does.StartWith("I currently have an ServiceStack Service that does nothing but relay requests"));
        Assert.That(result.Tags, Is.EquivalentTo(new[]{ "c#", "servicestack" }));
    }

    [Test]
    public async Task Can_import_from_stackoverflow_url()
    {
        var command = new ImportQuestionCommand(appConfig);

        await command.ExecuteAsync(new ImportQuestion
        {
            Site = ImportSite.StackOverflow,
            Url = "https://stackoverflow.com/questions/37329080/servicestack-as-passthru-to-another-servicestack-service",
        });

        var result = command.Result!;
        Assert.That(result.Title, Is.EqualTo("ServiceStack - As passthru to another ServiceStack service"));
        Assert.That(result.Body, Does.StartWith("I currently have an ServiceStack Service that does nothing but relay requests"));
        Assert.That(result.Tags, Is.EquivalentTo(new[]{ "c#", "servicestack" }));
        Assert.That(result.RefId, Is.EqualTo("stackoverflow.com:37329080"));
    }

    [Test]
    public async Task Can_import_from_shared_stackoverflow_url()
    {
        var command = new ImportQuestionCommand(appConfig);

        await command.ExecuteAsync(new ImportQuestion
        {
            Site = ImportSite.StackOverflow,
            Url = "https://stackoverflow.com/q/37329080/85785",
        });

        var result = command.Result!;
        Assert.That(result.Title, Is.EqualTo("ServiceStack - As passthru to another ServiceStack service"));
        Assert.That(result.Body, Does.StartWith("I currently have an ServiceStack Service that does nothing but relay requests"));
        Assert.That(result.Tags, Is.EquivalentTo(new[]{ "c#", "servicestack" }));
        Assert.That(result.RefId, Is.EqualTo("stackoverflow.com:37329080"));
    }

    [Test]
    public async Task Can_import_from_server_fault()
    {
        var command = new ImportQuestionCommand(appConfig);

        await command.ExecuteAsync(new ImportQuestion
        {
            Site = ImportSite.StackOverflow,
            Url = "https://serverfault.com/questions/1156725/fail2ban-bans-ip-addresses-yet-they-still-appear-in-access-log",
        });

        var result = command.Result!;
        Assert.That(result.Title, Is.EqualTo("fail2ban bans IP addresses, yet they still appear in access.log"));
        Assert.That(result.Body, Does.StartWith("This is my filter:"));
        Assert.That(result.Tags, Is.EquivalentTo(new[]{ "nginx", "iptables", "docker", "fail2ban" }));
        Assert.That(result.RefId, Is.EqualTo("serverfault.com:1156725"));
    }

    [Test]
    public async Task Can_import_from_askubuntu()
    {
        var command = new ImportQuestionCommand(appConfig);

        await command.ExecuteAsync(new ImportQuestion
        {
            Site = ImportSite.StackOverflow,
            Url = "https://askubuntu.com/questions/1509058/input-delay-on-terminal-ubuntu-22-04-4",
        });

        var result = command.Result!;
        Assert.That(result.Title, Is.EqualTo("input delay on Terminal Ubuntu 22.04.4"));
        Assert.That(result.Body, Does.StartWith("I have been using Ubuntu since Christmas,"));
        Assert.That(result.Tags, Is.EquivalentTo(new[]{ "command-line", "gnome-terminal" }));
        Assert.That(result.RefId, Is.EqualTo("askubuntu.com:1509058"));
    }

    [Test]
    public async Task Can_import_from_reddit()
    {
        var command = new ImportQuestionCommand(appConfig);

        await command.ExecuteAsync(new ImportQuestion
        {
            Site = ImportSite.Reddit,
            Url = "https://www.reddit.com/r/dotnet/comments/1byolum/all_the_net_tech_i_use_what_else_is_out_there/",
        });

        var result = command.Result!;
        Assert.That(result.Title, Is.EqualTo("All the .NET tech I use. What else is out there that is a must-have?"));
        Assert.That(result.Body, Does.StartWith("Sitting here tonight writing down everything in my technical stack"));
        Assert.That(result.Tags, Is.EquivalentTo(new[]{ "bit", "dump", "oop", "solid", "stack" }));
        Assert.That(result.RefId, Is.EqualTo("reddit.dotnet:1byolum"));
    }
}