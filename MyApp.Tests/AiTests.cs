using MyApp.ServiceInterface;
using NUnit.Framework;

namespace MyApp.Tests;

public class AiTests
{
    [Test]
    public void Can_parse_ModelRank_Response()
    {
        var json = """
                   Some text
                   {"reason":"The Reason","score":2}
                   """;
        var (reason, score) = json.ParseRankResponse()!;
        Assert.That(reason, Is.EqualTo("The Reason"));
        Assert.That(score, Is.EqualTo(2));
    }
}
