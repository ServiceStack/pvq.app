using ServiceStack;
using ServiceStack.Script;
using CreatorKit.ServiceModel;
using MyApp.ServiceModel;
using ServiceStack.OrmLite;

namespace CreatorKit.ServiceInterface;

public class EmailRenderersServices(EmailRenderer renderer) : Service
{
    public async Task<object> Any(PreviewEmail request)
    {
        var args = new Dictionary<string,object>(request.RequestArgs ?? new(), StringComparer.OrdinalIgnoreCase);
        
        var defaultValues = new Dictionary<string, object>
        {
            [nameof(RenderEmailBase.Email)] = "email@example.org",
            [nameof(RenderEmailBase.FirstName)] = "First",
            [nameof(RenderEmailBase.LastName)] = "Last",
            [nameof(RenderEmailBase.ExternalRef)] = "0123456789"
        };
        foreach (var entry in defaultValues)
        {
            args.TryAdd(entry.Key, entry.Value);
        }

        var renderRequestType = request.Renderer != null
            ? HostContext.Metadata.GetRequestType(request.Renderer)
            : null;

        if (renderRequestType == null && request.Request != null)
        {
            var requestType = HostContext.Metadata.GetRequestType(request.Request);
            var rendererAttr = requestType.FirstAttribute<RendererAttribute>();
            renderRequestType = rendererAttr?.Type;
            if (rendererAttr?.Layout != null)
                args.TryAdd(nameof(RenderCustomHtml.Layout), rendererAttr.Layout);
            if (rendererAttr?.Template != null)
                args.TryAdd(nameof(RenderCustomHtml.Template), rendererAttr.Template);
        }
        renderRequestType ??= typeof(RenderSimpleText);

        var renderRequest = args.FromObjectDictionary(renderRequestType);
        var response = await HostContext.ServiceController.ExecuteAsync(renderRequest, Request);
        return response;
    }

    public async Task<object> Any(RenderSimpleText request)
    {
        var context = renderer.CreateScriptContext();
        var evalBody = await context.RenderScriptAsync(request.Body, request.ToObjectDictionary());
        return evalBody;
    }

    public async Task<object> Any(RenderCustomHtml request)
    {
        var context = renderer.CreateMailContext(layout:request.Layout, page:request.Template);
        var evalBody = !string.IsNullOrEmpty(request.Body) 
            ? await context.RenderScriptAsync(request.Body, request.ToObjectDictionary())
            : string.Empty;

        using var db = HostContext.AppHost.GetDbConnection(Databases.CreatorKit);
        return await renderer.RenderToHtmlResultAsync(db, context, request, 
            args:new() {
                ["body"] = evalBody,
            });
    }

    public async Task<object> Any(RenderTagQuestionsEmail request)
    {
        var context = renderer.CreateMailContext(layout:"tags", page:"tagged-questions");

        var posts = await Db.SelectAsync(Db.From<Post>()
            .Where(x => x.CreationDate >= request.Date && x.CreationDate < request.Date.AddDays(1))
            .Where("replace(replace(tags,'[',','),']',',') LIKE '%,' || {0} || ',%'", request.Tag)
            .Limit(10));
        
        using var db = HostContext.AppHost.GetDbConnection(Databases.CreatorKit);
        return await renderer.RenderToHtmlResultAsync(db, context, request, 
            args:new() {
                ["tag"] = request.Tag,
                ["date"] = request.Date.ToString("MMMM dd"),
                [nameof(posts)] = posts,
            });
    }
}