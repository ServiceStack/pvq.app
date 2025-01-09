using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MyApp.Data;
using MyApp.ServiceInterface.App;
using MyApp.ServiceModel;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;

namespace MyApp.Tests;

public class ImportTests
{
    AppConfig appConfig;
    
    private static AppConfig CreateAppConfig()
    {
        var hostDir = TestUtils.GetHostDir();
        var allTagsFile = new FileInfo(Path.GetFullPath(Path.Combine(hostDir, "wwwroot", "data", "tags.txt")));
        
        var to = new AppConfig
        {
            RedditClient = Environment.GetEnvironmentVariable("REDDIT_CLIENT"),
            RedditSecret = Environment.GetEnvironmentVariable("REDDIT_SECRET")
        };
        to.LoadTags(allTagsFile);
        return to;
    }

    public ImportTests()
    {
        appConfig = CreateAppConfig();
    }

    private ImportQuestionCommand CreateCommand() => new(new NullLogger<ImportQuestionCommand>(), appConfig);

    [Test]
    public async Task Can_import_from_discourse_url()
    {
        var command = CreateCommand();

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
        var command = CreateCommand();

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
        var command = CreateCommand();

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
        var command = CreateCommand();

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
        var command = CreateCommand();

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
        var command = CreateCommand();

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

    record RedditTest(string Url, string Title, string BodyPrefix, string[] Tags, string RefUrn);
    RedditTest[] RedditTests =
    [
        new RedditTest(
            "https://www.reddit.com/r/dotnet/comments/1byolum/all_the_net_tech_i_use_what_else_is_out_there/",
            "All the .NET tech I use. What else is out there that is a must-have?",
            "Sitting here tonight writing down everything in my technical stack",
            [".net", "stack", "bit", "dump", "oop"],
            "reddit.dotnet:1byolum"
        ),
        new RedditTest(
            "https://www.reddit.com/r/dotnet/comments/1cfr36q/rpc_calls_state_of_the_art/",
            "RPC calls - state of the art",
            "Hi",
            [".net", "database", "client", ".net", "odbc"],
            "reddit.dotnet:1cfr36q"
        )
    ];

    [Explicit("Requires REDDIT_CLIENT and REDDIT_SECRET env vars")]
    [Test]
    public async Task Can_import_from_reddit()
    {
        var command = CreateCommand();
        
        foreach (var reddit in RedditTests)
        {
            await command.ExecuteAsync(new ImportQuestion
            {
                Site = ImportSite.Reddit,
                Url = reddit.Url,
            });

            var result = command.Result!;
            Assert.That(result.Title, Is.EqualTo(reddit.Title));
            Assert.That(result.Body, Does.StartWith(reddit.BodyPrefix));
            Assert.That(result.Tags, Is.EquivalentTo(reddit.Tags));
            Assert.That(result.RefUrn, Is.EqualTo(reddit.RefUrn));
        }
    }

    [Explicit("Requires REDDIT_CLIENT and REDDIT_SECRET env vars")]
    [Test]
    public async Task Can_request_using_OAuth()
    {
        var redditOAuthUrl = "https://www.reddit.com/api/v1/access_token";
        var uuid = Guid.NewGuid().ToString("N");
        Dictionary<string, object> postData = new()
        {
            ["grant_type"] = "client_credentials",
            ["device_id"] = uuid,
        };
        var response = await redditOAuthUrl.PostToUrlAsync(postData, requestFilter: req =>
        {
            req.AddBasicAuth(Environment.GetEnvironmentVariable("REDDIT_CLIENT")!, Environment.GetEnvironmentVariable("REDDIT_SECRET")!); 
            req.AddHeader("User-Agent", "pvq.app");
        });

        var obj = (Dictionary<string,object>)JSON.parse(response);
        obj.PrintDump();
        var accessToken = (string)obj["access_token"];
        var json = await "https://oauth.reddit.com/r/dotnet/comments/1byolum/all_the_net_tech_i_use_what_else_is_out_there.json"
            .GetJsonFromUrlAsync(requestFilter: req =>
            {
                req.AddBearerToken(accessToken);
                req.AddHeader("User-Agent", "pvq.app");
            });

        json.Print();
    }

    [Explicit("Requires curl")]
    [Test]
    public async Task Can_call_curl_to_get_url()
    {
        var url = "https://www.reddit.com/r/dotnet/comments/1byolum/all_the_net_tech_i_use_what_else_is_out_there.json";
        var args = new[]
        {
            $"curl -s '{url}'",
            "-H 'accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7'",
            "-H 'accept-language: en-US,en;q=0.9'",
            "-H 'cache-control: max-age=0'",
            "-H 'dnt: 1'",
            "-H 'priority: u=0, i'",
            "-H 'upgrade-insecure-requests: 1'",
            "-H 'user-agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36'"
        }.ToList();
        if (Env.IsWindows)
        {
            args = args.Map(x => x.Replace('\'', '"'));
        }
        
        var argsString = string.Join(" ", args);
        var sb = StringBuilderCache.Allocate();
        await ProcessUtils.RunShellAsync(argsString, onOut:line => sb.AppendLine(line));
        var result = StringBuilderCache.ReturnAndFree(sb);
        Assert.That(result.Trim(), Does.StartWith("[{"));
    }

    private static string Password = Environment.GetEnvironmentVariable("AUTH_SECRET") ?? "p@55wOrd";
    
    [Explicit("Run Manually")]
    [Test]
    public async Task Can_create_new_users()
    {
        var client = await TestUtils.CreateAdminProdClientAsync();
        
        var api = await client.ApiAsync(new EnsureApplicationUser
        {
            UserName = "phi4",
            Email = "servicestack.mail+phi4@gmail.com",
            DisplayName = "Phi 4 14B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ph/phi4/phi4.svg",
            Model = "phi4",
            Password = Password, 
        });
        
        api.ThrowIfError();
        api.Response.PrintDump();
    }
}