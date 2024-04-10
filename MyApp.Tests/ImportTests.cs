using MyApp.Data;
using MyApp.ServiceInterface.App;
using MyApp.ServiceModel;
using NUnit.Framework;
using ServiceStack.Text;

namespace MyApp.Tests;

public class ImportTests
{
    private static AppConfig CreateAppConfig()
    {
        var hostDir = TestUtils.GetHostDir();
        var allTagsFile = new FileInfo(Path.GetFullPath(Path.Combine(hostDir, "wwwroot", "data", "tags.txt")));
        

        var appConfig = new AppConfig();
        appConfig.LoadTags(allTagsFile);
        return appConfig;
    }

    [Test]
    public async Task Can_import_from_discourse_url()
    {
        var appConfig = CreateAppConfig();
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
    }

    [Test]
    public async Task Can_import_from_discourse_answer_url()
    {
        var appConfig = CreateAppConfig();
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
        Assert.That(result.Tags.Count, Is.GreaterThan(0));
    }
}