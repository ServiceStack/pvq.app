using System.Net.Mail;
using Microsoft.Extensions.Logging;
using ServiceStack;
using MyApp.Data;

namespace MyApp.ServiceInterface.Recurring;

public abstract class SendEmailCommand(ILogger<LogCommand> log, SmtpConfig config) : SyncCommand<SendEmail>
{
    private static long count = 0;
    protected override void Run(SendEmail request)
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
    }
}
