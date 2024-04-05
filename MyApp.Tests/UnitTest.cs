using NUnit.Framework;
using ServiceStack;
using ServiceStack.Testing;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.Tests;

public class UnitTest
{
    private readonly ServiceStackHost appHost;

    public UnitTest()
    {
        appHost = new BasicAppHost().Init();
        appHost.Container.AddTransient<MyServices>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    [Test]
    public void Can_call_MyServices()
    {
        var service = appHost.Container.Resolve<MyServices>();

        var response = (HelloResponse)service.Any(new Hello { Name = "World" });

        Assert.That(response.Result, Is.EqualTo("Hello, World!"));
    }

    [Test]
    public void Find_UserNames_in_Text()
    {
        var text = "There was @alice and @Bob and @charlie.\n@david-dee was there too @5. paging @mythz";
        var userNames = text.FindUserNameMentions();
        
        Assert.That(userNames, Is.EquivalentTo(new[]{ "alice", "charlie", "david-dee", "mythz" }));
    }
}
