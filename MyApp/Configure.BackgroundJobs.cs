using MyApp.Data;
using MyApp.ServiceInterface.App;
using MyApp.ServiceInterface.Recurring;
using MyApp.ServiceModel;
using ServiceStack.Jobs;

[assembly: HostingStartup(typeof(MyApp.ConfigureBackgroundJobs))]

namespace MyApp;

public class ConfigureBackgroundJobs : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            services.AddPlugin(new CommandsFeature());
            services.AddPlugin(new BackgroundsJobFeature());
            services.AddHostedService<JobsHostedService>();
        }).ConfigureAppHost(afterAppHostInit:appHost =>
        {
            var jobs = appHost.Resolve<IBackgroundJobs>();
            
            jobs.RecurringCommand<LogCommand>("Every Minute", Schedule.EveryMinute);
            
            jobs.RecurringCommand<SendEmailCommand>("Every 8 hours", Schedule.Interval(TimeSpan.FromHours(8)), 
                new SendEmail
                {
                    To = "demis.bellot@gmail.com",
                    Subject = "Hi from recurring command",
                    BodyText = "Test email from a Scheduled Tasks recurring command"
                });
            
            jobs.RecurringApi("DbWrites Hourly", Schedule.Hourly,
                new DbWrites {
                    PeriodicTasks = new PeriodicTasks { PeriodicFrequency = PeriodicFrequency.Hourly }
                });

        });
}

public class JobsHostedService(ILogger<JobsHostedService> log, IBackgroundJobs jobs) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await jobs.StartAsync(stoppingToken);
        
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        var tick = 0;
        var errors = 0;
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                tick++;
                await jobs.TickAsync();
            }
            catch (Exception e)
            {
                log.LogError(e, "JOBS {Errors}/{Tick} Error in JobsHostedService: {Message}", 
                    ++errors, tick, e.Message);
            }
        }
    }
}