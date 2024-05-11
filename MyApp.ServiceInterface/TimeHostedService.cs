using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack.Messaging;

namespace MyApp.ServiceInterface;

public class TimedHostedService(ILogger<TimedHostedService> logger, IMessageService mqServer) : IHostedService, IDisposable
{
    private int EveryMins = 60;
    private int executionCount = 0;
    private Timer? timer = null;

    public Task StartAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Timed Hosted Service running");

        timer = new Timer(DoWork, null, TimeSpan.Zero,
            TimeSpan.FromMinutes(EveryMins));

        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        var count = Interlocked.Increment(ref executionCount);
        logger.LogInformation("Timed Hosted Service is working. Count: {Count}", count);
        
        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogInformation("MQ Worker running at: {Stats}\n", mqServer.GetStatsDescription());
        
        var frequentTasks = new PeriodicTasks { PeriodicFrequency = PeriodicFrequency.Hourly };
        using var mq = mqServer.MessageFactory.CreateMessageProducer();
        mq.Publish(new DbWrites { PeriodicTasks = frequentTasks });
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Timed Hosted Service is stopping");

        timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        timer?.Dispose();
    }
}
