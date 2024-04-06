using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MyApp.Data;
using ServiceStack;

namespace MyApp.ServiceInterface;

public abstract class MqServicesBase(ILogger log, AppConfig appConfig) : Service
{
    protected ILogger Log => log;
    protected AppConfig AppConfig => appConfig;
    
    protected async Task ExecuteAsync<T>(IExecuteCommandAsync<T> command, T request) where T : class
    {
        var commandName = command.GetType().Name;
        var sw = Stopwatch.StartNew();
        try
        {
            await command.ExecuteAsync(request);
            log.LogDebug("{Command} took {ElapsedMilliseconds}ms to execute", commandName, sw.ElapsedMilliseconds);

            appConfig.AddCommandResult(new() {
                Name = commandName,
                Ms = sw.ElapsedMilliseconds,
                At = DateTime.UtcNow,
            });
        }
        catch (Exception e)
        {
            var requestBody = request.ToJsv();
            log.LogError(e, "{Command}({Request}) failed: {Message}", commandName, requestBody, e.Message);
            
            appConfig.AddCommandResult(new() {
                Name = commandName,
                Ms = sw.ElapsedMilliseconds,
                At = DateTime.UtcNow,
                Request = requestBody,
                Error = e.Message,
            });
        }
    }
}
