using AiServer.ServiceModel;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace MyApp.Tests;

[Explicit("Requires Local AI Server")]
public class AiServerTests
{
    [Test]
    public void Can_QueryApiModels()
    {
        var client = TestUtils.CreateAiDevClient();
        var api = client.Api(new QueryApiModels());
        api.ThrowIfError();
        api.Response.PrintDump();
    }
}