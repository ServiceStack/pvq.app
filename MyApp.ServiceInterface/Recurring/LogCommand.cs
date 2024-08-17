using Microsoft.Extensions.Logging;
using ServiceStack;

namespace MyApp.ServiceInterface.Recurring;

public class LogCommand(ILogger<LogCommand> log) : SyncCommand
{
    private static long count = 0;
    protected override void Run()
    {
        Interlocked.Increment(ref count);
        log.LogInformation("Log {Count}: Hello from Recurring Command", count);
    }
}
