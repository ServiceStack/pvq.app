using ServiceStack.Messaging;
using MyApp.ServiceModel;
using Microsoft.AspNetCore.Identity;
using MyApp.Data;
using MyApp.ServiceInterface;

[assembly: HostingStartup(typeof(MyApp.ConfigureMq))]

namespace MyApp;

/**
 * Register ServiceStack Services you want to be able to invoke in a managed Background Thread
 * https://docs.servicestack.net/background-mq
*/
public class ConfigureMq : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            var smtpConfig = context.Configuration.GetSection(nameof(SmtpConfig))?.Get<SmtpConfig>();
            if (smtpConfig is not null)
            {
                services.AddSingleton(smtpConfig);
            }
            services.AddSingleton<IMessageService>(c => new BackgroundMqService());
            services.AddSingleton<IMessageProducer>(c => c.GetRequiredService<IMessageService>().MessageFactory.CreateMessageProducer());
            services.AddSingleton<ModelWorkerQueue>();
            services.AddSingleton<WorkerAnswerNotifier>();
            // services.AddPlugin(new CommandsFeature());
            // Use ServiceStack.Jobs Recurring Tasks instead
            // services.AddHostedService<TimedHostedService>();
        })
        .ConfigureAppHost(afterAppHostInit: appHost => {
            var mqService = appHost.Resolve<IMessageService>();

            //Register ServiceStack APIs you want to be able to invoke via MQ
            mqService.RegisterHandler<SendEmail>(appHost.ExecuteMessage);
            mqService.RegisterHandler<RenderComponent>(appHost.ExecuteMessage);
            mqService.RegisterHandler<DiskTasks>(appHost.ExecuteMessage);
            mqService.RegisterHandler<AnalyticsTasks>(appHost.ExecuteMessage);
            mqService.RegisterHandler<DbWrites>(appHost.ExecuteMessage);
            mqService.RegisterHandler<SearchTasks>(appHost.ExecuteMessage);
            mqService.RegisterHandler<AiServerTasks>(appHost.ExecuteMessage);
            mqService.Start();
        });
}

/// <summary>
/// Sends emails by publishing a message to the Background MQ Server where it's processed in the background
/// </summary>
public class EmailSender(IMessageService messageService) : IEmailSender<ApplicationUser>
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        using var mqClient = messageService.CreateMessageProducer();
        mqClient.Publish(new SendEmail
        {
            To = email,
            Subject = subject,
            BodyHtml = htmlMessage,
        });

        return Task.CompletedTask;
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendEmailAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendEmailAsync(email, "Reset your password", $"Please reset your password using the following code: {resetCode}");
}
