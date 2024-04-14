using System;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using ServiceStack;
using CreatorKit.ServiceModel;
using CreatorKit.ServiceModel.Types;
using MyApp.Data;

namespace CreatorKit.ServiceInterface;

public class EmailProvider(SmtpConfig config)
{
    public Action<EmailMessage, System.Net.Mail.MailMessage> BeforeSend { get; set; }

    public Action<EmailMessage, Exception> OnException { get; set; }

    public void Send(EmailMessage email)
    {
        try
        {
            using var client = new SmtpClient(config.Host, config.Port);
            client.Credentials = new System.Net.NetworkCredential(config.Username, config.Password);
            client.EnableSsl = true;

            var emailTo = email.To.First().ToMailAddress();
            var emailFrom = (email.From ?? new MailTo { Email = config.FromEmail, Name = config.FromName! }).ToMailAddress();

            var msg = new System.Net.Mail.MailMessage(emailFrom, emailTo)
            {
                Subject = email.Subject,
                Body = email.BodyText ?? email.BodyHtml,
                IsBodyHtml = email.BodyText == null,
            };

            if (!msg.IsBodyHtml && email.BodyHtml != null)
            {
                var mimeType = new ContentType(MimeTypes.Html);
                var alternate = AlternateView.CreateAlternateViewFromString(email.BodyHtml, mimeType);
                msg.AlternateViews.Add(alternate);
            }

            foreach (var to in email.To.Skip(1))
            {
                msg.To.Add(to.ToMailAddress());
            }
            foreach (var cc in email.Cc.Safe())
            {
                msg.CC.Add(cc.ToMailAddress());
            }
            foreach (var bcc in email.Bcc.Safe())
            {
                msg.Bcc.Add(bcc.ToMailAddress());
            }
            if (!string.IsNullOrEmpty(config.Bcc))
            {
                msg.Bcc.Add(new MailAddress(config.Bcc));
            }

            BeforeSend?.Invoke(email, msg);

            client.Send(msg);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}

public static class EmailProviderUtils
{
    public static MailAddress ToMailAddress(this MailTo from)
    {
        return string.IsNullOrEmpty(from.Name)
            ? new MailAddress(from.Email)
            : new MailAddress(from.Email, from.Name);
    }
}