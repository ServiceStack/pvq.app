using System.Net.Mail;
using Microsoft.Extensions.Logging;
using MyApp.Data;
using ServiceStack;

namespace MyApp.ServiceInterface.App;

public class LogRequest
{
    public string Message { get; set; }
}

public class LogCommand(ILogger<LogCommand> log) : IAsyncCommand<LogRequest>
{
    private static int count = 0;
    
    public Task ExecuteAsync(LogRequest request)
    {
        Interlocked.Increment(ref count);
        log.LogInformation("Log {Count}: {Message}", count, request.Message);
        return Task.CompletedTask;
    }
}

public class SendEmailCommand(ILogger<LogCommand> log, SmtpConfig config) : IAsyncCommand<SendEmail>
{
    private static int count = 0;

    public Task ExecuteAsync(SendEmail request)
    {
        Interlocked.Increment(ref count);
        log.LogInformation("Sending {Count} email to {Email} with subject {Subject}", count, request.To, request.Subject);

        using var client = new SmtpClient(config.Host, config.Port);
        client.Credentials = new System.Net.NetworkCredential(config.Username, config.Password);
        client.EnableSsl = true;

        // If DevToEmail is set, send all emails to that address instead
        var emailTo = config.DevToEmail != null
            ? new MailAddress(config.DevToEmail)
            : new MailAddress(request.To, request.ToName);

        var emailFrom = new MailAddress(config.FromEmail, config.FromName);

        var msg = new MailMessage(emailFrom, emailTo)
        {
            Subject = request.Subject,
            Body = request.BodyHtml ?? request.BodyText,
            IsBodyHtml = request.BodyHtml != null,
        };

        if (config.Bcc != null)
        {
            msg.Bcc.Add(new MailAddress(config.Bcc));
        }

        client.Send(msg);
        return Task.CompletedTask;
    }
}
