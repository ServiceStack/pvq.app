using System.Threading.Tasks;
using ServiceStack;
using CreatorKit.ServiceModel;
using CreatorKit.ServiceModel.Types;
using MyApp.ServiceModel;
using ServiceStack.OrmLite;
using ServiceStack.Web;

namespace CreatorKit.ServiceInterface;

public class EmailServices(EmailRenderer renderer) : Service
{
    public async Task<object> Any(UpdateMailMessageDraft request)
    {
        using var db = HostContext.AppHost.GetDbConnection(Databases.CreatorKit);
        var message = db.SingleById<MailMessage>(request.Id);
        var renderRequestType = HostContext.Metadata.GetRequestType(message.Renderer);
        message.Layout = request.Layout;
        message.Template = request.Template;
        message.Message.Subject = request.Subject;
        message.Message.Body = request.Body;
        message.Draft = request.Send != true;

        message.RendererArgs.Set(nameof(message.Layout), message.Layout);
        message.RendererArgs.Set(nameof(message.Template), message.Template);
        message.RendererArgs.Set(nameof(message.Message.Body), message.Message.Body);

        var renderRequest = message.RendererArgs.FromObjectDictionary(renderRequestType);
        var response = await HostContext.ServiceController.ExecuteAsync(renderRequest, Request);
        if (response is IHttpError httpError)
            return httpError;
        var responseBody = response is IHttpResult httpResult ? httpResult.Response.ToString() : response.ToString();

        if (message.Renderer == nameof(RenderSimpleText))
            message.Message.BodyText = responseBody;
        else
            message.Message.BodyHtml = responseBody;

        db.Update(message);

        if (request.Send == true)
        {
            renderer.SendMailMessage(message.Id);
        }

        return message;
    }

    public async Task<object> Any(SimpleTextEmail request)
    {
        using var db = HostContext.AppHost.GetDbConnection(Databases.CreatorKit);
        var contact = db.GetOrCreateContact(request);
        var viewRequest = request.ConvertTo<RenderSimpleText>().FromContact(contact);
        var bodyText = (string)await Gateway.SendAsync(typeof(string), viewRequest);

        var email = renderer.CreateMessage(db, new MailMessage
        {
            Draft = request.Draft ?? false,
            Message = new EmailMessage
            {
                To = contact.ToMailTos(),
                Subject = request.Subject,
                Body = request.Body,
                BodyText = bodyText,
            },
        }.FromRequest(request));
        return email;
    }

    public async Task<object> Any(CustomHtmlEmail request)
    {
        using var db = HostContext.AppHost.GetDbConnection(Databases.CreatorKit);
        var contact = db.GetOrCreateContact(request);
        var viewRequest = request.ConvertTo<RenderCustomHtml>().FromContact(contact);
        var bodyHtml = (string)await Gateway.SendAsync(typeof(string), viewRequest);

        var email = renderer.CreateMessage(db, new MailMessage
        {
            Draft = request.Draft ?? false,
            Message = new EmailMessage
            {
                To = contact.ToMailTos(),
                Subject = request.Subject,
                Body = request.Body,
                BodyHtml = bodyHtml,
            },
        }.FromRequest(viewRequest));
        return email;
    }

    public async Task<object> Any(TagQuestionsEmail request)
    {
        using var db = HostContext.AppHost.GetDbConnection(Databases.CreatorKit);
        var contact = db.GetOrCreateContact(request);
        var viewRequest = request.ConvertTo<RenderTagQuestionsEmail>().FromContact(contact);
        var bodyHtml = (string)await Gateway.SendAsync(typeof(string), viewRequest);

        var email = renderer.CreateMessage(db, new MailMessage
        {
            Draft = request.Draft ?? false,
            Message = new EmailMessage
            {
                To = contact.ToMailTos(),
                Subject = $"New {request.Tag} questions for {request.Date:MMMM dd} - pvq.app",
                BodyHtml = bodyHtml,
            },
        }.FromRequest(viewRequest));
        return email;
    }
}
