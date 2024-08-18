using Microsoft.AspNetCore.Identity;
using MyApp.Data;
using MyApp.ServiceInterface.Recurring;
using ServiceStack.Jobs;

namespace MyApp.ServiceInterface;

/// <summary>
/// Sends emails by publishing a message to the Background MQ Server where it's processed in the background
/// </summary>
public class EmailSender(IBackgroundJobs jobs) : IEmailSender<ApplicationUser>
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        jobs.RunCommand<SendEmailCommand>(new SendEmail
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
