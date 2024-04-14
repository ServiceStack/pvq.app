using System.Net;
using CreatorKit.ServiceInterface;
using CreatorKit.ServiceModel;
using ServiceStack.IO;
using ServiceStack.Messaging;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Web;
using MyApp.Data;
using MyApp.ServiceModel;

[assembly: HostingStartup(typeof(MyApp.ConfigureCreatorKit))]

namespace MyApp;

public class ConfigureCreatorKit : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) =>
        {
            AppData.Set(context.Configuration);
            services.AddSingleton(AppData.Instance);
            services.AddSingleton(c => EmailRenderer.Create(
                c.GetRequiredService<IVirtualFiles>(), c.GetRequiredService<IMessageService>()));
            services.AddSingleton<EmailProvider>();
        })
        .ConfigureAppHost(appHost =>
        {
            var mqService = appHost.Resolve<IMessageService>();
            mqService.RegisterHandler<CreatorKitTasks>(appHost.ExecuteMessage);

            appHost.Config.AddRedirectParamsToQueryString = true;
            appHost.Config.AllowFileExtensions.Add("json");
            MarkdownConfig.Transformer = new MarkdigTransformer();
            LoadAsync(appHost).GetAwaiter().GetResult();
        });

    private static async Task LoadAsync(ServiceStackHost appHost)
    {
        await appHost.Resolve<AppData>().LoadAsync(appHost, 
            appHost.ContentRootDirectory.GetDirectory("emails"), appHost.RootDirectory.GetDirectory("img/mail"));
        appHost.ScriptContext.ScriptAssemblies.Add(typeof(Hello).Assembly);
        appHost.ScriptContext.ScriptMethods.Add(new ValidationScripts());
        appHost.ScriptContext.Args[nameof(AppData)] = AppData.Instance;
    }
}

public class ValidationScripts : ScriptMethods
{
    public ITypeValidator ActiveUser() => new ActiveUserValidator();
}

public class ActiveUserValidator : TypeValidator, IAuthTypeValidator
{
    public static string DefaultErrorMessage { get; set; } = "Your account is locked";
    public ActiveUserValidator()
        : base(nameof(HttpStatusCode.Forbidden), DefaultErrorMessage, 403)
    {
        this.ContextArgs = new Dictionary<string, object>();
    }

    public override async Task ThrowIfNotValidAsync(object dto, IRequest request)
    {
        await IsAuthenticatedValidator.Instance.ThrowIfNotValidAsync(dto, request).ConfigAwait();
        
        // var session = await request.SessionAsAsync<CustomUserSession>();
        // var userId = session.UserAuthId.ToInt();
        // var appData = request.TryResolve<AppData>();
        // var checkDb = appData.BannedUsersMap.ContainsKey(userId) || session.LockedDate != null ||
        //               (session.BanUntilDate != null && session.BanUntilDate > DateTime.UtcNow);
        // if (checkDb)
        // {
        //     using var db = HostContext.AppHost.GetDbConnection(request);
        //     var user = await db.SingleByIdAsync<ApplicationUser>(userId);
        //     if (user == null)
        //         throw new HttpError(ResolveStatusCode(), ResolveErrorCode(), "Your account no longer exists");
        //     
        //     if (user.BanUntilDate != null && user.BanUntilDate > DateTime.UtcNow)
        //         throw new HttpError(ResolveStatusCode(), ResolveErrorCode(), 
        //             $"Your account will be unbanned in {(user.BanUntilDate.Value - DateTime.UtcNow).Humanize()}");
        //
        //     if (user.LockedDate != null)
        //         throw new HttpError(ResolveStatusCode(), ResolveErrorCode(), ResolveErrorMessage(request, dto));
        //
        //     appData.BannedUsersMap.TryRemove(userId, out _);
        // }
    }
}

public class MarkdigTransformer : IMarkdownTransformer
{
    private Markdig.MarkdownPipeline Pipeline { get; } = 
        Markdig.MarkdownExtensions.UseAdvancedExtensions(new Markdig.MarkdownPipelineBuilder()).Build();
    public string Transform(string markdown) => Markdig.Markdown.ToHtml(markdown, Pipeline);
}