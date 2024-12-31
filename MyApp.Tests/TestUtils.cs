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
    public static async Task<JsonApiClient> CreateAdminDevClientAsync()
    {
        var client = CreateDevClient();
        await client.ApiAsync(new Authenticate
        {
            provider = "credentials",
            UserName = "admin",
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
    public static JsonApiClient CreateAdminProdClient() => new("https://pvq.app")
    {
        BearerToken = Environment.GetEnvironmentVariable("AUTH_SECRET")
    };
    
    public static async Task<JsonApiClient> CreateAdminProdClientAsync()
    {
        var client = CreateProdClient();
        await client.ApiAsync(new Authenticate
        {
            provider = "credentials",
            UserName = "admin",
            Password = Environment.GetEnvironmentVariable("AUTH_SECRET")
        });
        return client;
    }

    public static JsonApiClient CreateAiDevClient() => new("https://localhost:5005")
    {
        BearerToken = Environment.GetEnvironmentVariable("AK_PVQ")
    };
    
    public static JsonApiClient CreateAiProdClient() => new("https://openai.servicestack.net")
    {
        BearerToken = Environment.GetEnvironmentVariable("AK_PVQ")
    };
  
}