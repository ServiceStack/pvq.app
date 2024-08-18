using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MyApp.Components.Shared;
using MyApp.Data;
using MyApp.ServiceInterface;
using MyApp.ServiceInterface.Renderers;
using MyApp.ServiceModel;

[assembly: HostingStartup(typeof(MyApp.ConfigureRenderer))]

namespace MyApp;

public class ConfigureRenderer : IHostingStartup
{
    static T RequiredService<T>() where T : notnull => 
        ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<T>();

    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context,services) =>
        {
            var svc = new ServiceCollection();
            svc.AddLogging();
            svc.AddSingleton<AppConfig>(c => RequiredService<AppConfig>());
            svc.AddSingleton<MarkdownQuestions>(c => RequiredService<MarkdownQuestions>());
            svc.AddSingleton<NavigationManager>(c => new StaticNavigationManager());
            svc.AddSingleton<ImageCreator>();
            var sp = svc.BuildServiceProvider();
            services.AddScoped<HtmlRenderer>(c => new HtmlRenderer(sp, c.GetRequiredService<ILoggerFactory>()));
            
            services.AddScoped<BlazorRenderer>();
            services.AddSingleton<RendererCache>();
            services.RegisterService<RenderServices>();

            services.AddTransient<RegenerateMetaCommand>();
            services.AddScoped<RenderQuestionPostCommand>();
            services.AddScoped<RenderHomeTabCommand>();
        })
        .ConfigureAppHost(appHost => { });
}

// Use fake NavigationManager in Static Rendering to avoid NavigationManager has not been initialized Exception
internal class StaticNavigationManager : NavigationManager
{
    public StaticNavigationManager()
    {
        Initialize("https://pvq.app/", "https://pvq.app/");
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        NotifyLocationChanged(false);
    }
}

[Tag(Tags.Renderer)]
public class RenderQuestionPostCommand(BlazorRenderer renderer, RendererCache cache) : IAsyncCommand<QuestionAndAnswers>
{
    public async Task ExecuteAsync(QuestionAndAnswers request)
    {
        var html = await renderer.RenderComponent<QuestionPost>(new() { ["Question"] = request });
        await cache.SetQuestionPostHtmlAsync(request.Id, html);
    }
}

public class RenderHome
{
    public string? Tab { get; set; }
    public List<Post> Posts { get; set; }
}
[Tag(Tags.Renderer)]
public class RenderHomeTabCommand(BlazorRenderer renderer, RendererCache cache) : IAsyncCommand<RenderHome>
{
    public async Task ExecuteAsync(RenderHome request)
    {
        var html = await renderer.RenderComponent<HomeTab>(new()
        {
            ["Tab"] = request.Tab,
            ["Posts"] = request.Posts,
        });
        await cache.SetHomeTabHtmlAsync(request.Tab, html);
    }
}

public class RenderServices(MarkdownQuestions markdown) : Service
{
    public object Any(PreviewMarkdown request)
    {
        var html = markdown.GenerateHtml(request.Markdown);
        return html;
    }
}
