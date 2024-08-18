using CreatorKit.ServiceInterface;
using ServiceStack;
using CreatorKit.ServiceModel;
using CreatorKit.ServiceModel.Types;
using Markdig;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;
using ServiceStack.Script;
using SendMessages = CreatorKit.ServiceInterface.SendMessages;

namespace MyApp.ServiceInterface;

public class EmailTemplateServices(AppConfig appConfig, QuestionsProvider questions, EmailRenderer renderer, IBackgroundJobs jobs) 
    : Service
{
    static string GetUnsubscribeFooter(string postUrl) => $"""
       <p style="font-size:small;color:#666">
           —<br>
           <a href="{postUrl}">view it on pvq.app</a>,
           or <a href="{postUrl.LastLeftPart('#')}?unsubscribe=1">unsubscribe</a>.<br>
           You are receiving this because you are subscribed to this post.
       </p>
    """;

    public async Task<object> Any(SendNewAnswerEmail request)
    {
        var answerFile = await questions.GetAnswerFileAsync(request.AnswerId);
        if (answerFile == null)
            throw HttpError.NotFound("Answer not found");

        var answer = await questions.GetAnswerAsPostAsync(answerFile);
        var postId = answer.ParentId;
        var post = await Db.SingleByIdAsync<Post>(postId);

        var user = await Db.SingleAsync(Db.From<ApplicationUser>().Where(x => x.UserName == request.UserName));
        var requestArgs = request.ToObjectDictionary();
        var contactArgs = user.ToContactArgs();
        var args = requestArgs.Merge(contactArgs);

        var context = renderer.CreateScriptContext();
        var bodyText = await context.RenderScriptAsync(answer.Body, args);
        var pipeline = EmailUtils.CreateEmailPipeline();
        var postUrl = $"{Request.GetBaseUrl()}/questions/{post.Id}/{post.Slug}#{request.AnswerId}";
        var bodyHtml = pipeline.RenderMarkdownForHtmlEmail(bodyText)
                     + GetUnsubscribeFooter(postUrl);

        using var db = HostContext.AppHost.GetDbConnection(Databases.CreatorKit);
        var email = new MailMessage
        {
            Draft = false,
            Message = new EmailMessage
            {
                To = contactArgs.ToMailTos(),
                From = new() { Email = appConfig.NotificationsEmail, Name = answer.CreatedBy! },
                Subject = $"[pvq.app] {answer.Title}",
                BodyText = bodyText,
                BodyHtml = bodyHtml,
            },
            CreatedDate = DateTime.UtcNow,
            ExternalRef = EmailUtils.CreateRef(),
        }.FromRequest(request);

        jobs.RunCommand<SendMessagesCommand>(new SendMessages {
            Messages = [email]
        });

        return new StringResponse
        {
            Result = "OK",
        };
    }
}

public static class EmailUtils
{
    public static string CreateRef() => Guid.NewGuid().ToString("N");

    public static MarkdownPipeline CreateEmailPipeline()
    {
        var builder = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions();
        var pipeline = builder.Build();
        return pipeline;
    }

    public static string RenderMarkdownForHtmlEmail(this MarkdownPipeline pipeline, string bodyText)
    {
        var writer = new StringWriter();
        var renderer = new Markdig.Renderers.HtmlRenderer(writer);
        pipeline.Setup(renderer);

        var document = Markdown.Parse(bodyText, pipeline);
        renderer.Render(document);
        var html = writer.ToString();
        return html;
    }

    public static MailTo ToMailTo(this ApplicationUser sub) => new()
    {
        Email = sub.Email!,
        Name = sub.DisplayName!,
    };

    public static List<MailTo> ToMailTos(this Contact sub) => [sub.ToMailTo()];

    public static Dictionary<string, object?> ToContactArgs(this ApplicationUser user)
    {
        var displayName = user.DisplayName ?? throw new ArgumentNullException(nameof(user.DisplayName));
        var firstName = displayName.LeftPart(' ');
        var lastName = displayName.IndexOf(' ') > 0
            ? displayName.LastRightPart(' ')
            : null;

        var args = new Dictionary<string, object?>
        {
            [nameof(Contact.FirstName)] = firstName,
            [nameof(Contact.LastName)] = lastName,
            [nameof(Contact.Email)] = user.Email,
            [nameof(Contact.ExternalRef)] = user.Id,
            [nameof(ApplicationUser.DisplayName)] = displayName
        };
        return args;
    }

    public static List<MailTo> ToMailTos(this Dictionary<string, object?> args)
    {
        var email = args[nameof(Contact.Email)] as string
                    ?? throw new ArgumentNullException(nameof(Contact.Email));
        var name = args.TryGetValue(nameof(ApplicationUser.DisplayName), out var oDisplayName)
            ? oDisplayName as string
            : null;
        if (name == null)
        {
            var firstName = args.TryGetValue(nameof(Contact.FirstName), out var oFirstName)
                ? oFirstName as string
                : null;
            var lastName = args.TryGetValue(nameof(Contact.LastName), out var oLastName) ? oLastName as string : null;
            name = firstName != null && lastName != null
                ? $"{firstName} {lastName}"
                : firstName ?? lastName ?? throw new ArgumentNullException(nameof(ApplicationUser.DisplayName));
        }

        return [new() { Email = email, Name = name }];
    }
}