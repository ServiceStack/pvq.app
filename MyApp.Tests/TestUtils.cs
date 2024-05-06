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
}