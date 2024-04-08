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

    [Test]
    public void Does_HumanReadable()
    {
        Assert.That(1.ToHumanReadable(), Is.EquivalentTo("1"));
        Assert.That(10.ToHumanReadable(), Is.EquivalentTo("10"));
        Assert.That(100.ToHumanReadable(), Is.EquivalentTo("100"));
        Assert.That(1000.ToHumanReadable(), Is.EquivalentTo("1k"));
        Assert.That(1100.ToHumanReadable(), Is.EquivalentTo("1.1k"));
        Assert.That(10000.ToHumanReadable(), Is.EquivalentTo("10k"));
        Assert.That(11000.ToHumanReadable(), Is.EquivalentTo("11k"));
        Assert.That(11100.ToHumanReadable(), Is.EquivalentTo("11.1k"));
        Assert.That(1000000.ToHumanReadable(), Is.EquivalentTo("1m"));
        Assert.That(1100000.ToHumanReadable(), Is.EquivalentTo("1.1m"));
        Assert.That(10000000.ToHumanReadable(), Is.EquivalentTo("10m"));
        Assert.That(11000000.ToHumanReadable(), Is.EquivalentTo("11m"));
        Assert.That(11100000.ToHumanReadable(), Is.EquivalentTo("11.1m"));
    }
}
