using ServiceStack;
using ServiceStack.Logging;

namespace MyApp.Tests;

public static class TestUtils
{
    public static string GetHostDir()
    {
        LogManager.LogFactory = new ConsoleLogFactory();
        return "../../../../MyApp";
        // var appSettings = JSON.parse(File.ReadAllText(Path.GetFullPath("appsettings.json")));
        // return appSettings.ToObjectDictionary()["HostDir"].ToString()!;
    }
    
    public static JsonApiClient CreateDevClient() => new("https://localhost:5001");
    public static async Task<JsonApiClient> CreateAuthenticatedDevClientAsync()
    {
        var client = CreateDevClient();
        await client.ApiAsync(new Authenticate
        {
            provider = "credentials",
            UserName = "mythz",
            Password = Environment.GetEnvironmentVariable("AUTH_SECRET")
        });
        return client;
    }
   
    public static JsonApiClient CreateProdClient() => new("https://pvq.app");
    public static async Task<JsonApiClient> CreateAuthenticatedProdClientAsync()
    {
        var client = CreateProdClient();
        await client.ApiAsync(new Authenticate
        {
            provider = "credentials",
            UserName = "mythz",
            Password = Environment.GetEnvironmentVariable("AUTH_SECRET")
        });
        return client;
    }
    
}