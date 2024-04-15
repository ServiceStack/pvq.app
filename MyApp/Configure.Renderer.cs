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

public class RenderQuestionPostCommand(BlazorRenderer renderer, RendererCache cache) : IAsyncCommand<QuestionAndAnswers>
{
    public async Task ExecuteAsync(QuestionAndAnswers request)
    {
        var html = await renderer.RenderComponent<QuestionPost>(new() { ["Question"] = request });
        await cache.SetQuestionPostHtmlAsync(request.Id, html);
    }
}

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

public class RenderServices(
    ILogger<RenderServices> log,
    MarkdownQuestions markdown,
    BlazorRenderer renderer, 
    RendererCache cache,
    ICommandExecutor executor) : Service
{
    public async Task Any(RenderComponent request)
    {
        // Runs at most once per minute per post from Question.razor page
        // var oncePerMinute = Cache.Add($"Question:{Id}", Id, TimeSpan.FromMinutes(1));

        if (request.RegenerateMeta != null)
        {
            var command = executor.Command<RegenerateMetaCommand>();
            await executor.ExecuteAsync(command, request.RegenerateMeta);

            // Result is used to determine if Question Post HTML needs to be regenerated
            request.Question = command.Question;
        }

        if (request.Question != null)
        {
            log.LogInformation("Rendering Question Post HTML {Id}...", request.Question.Id);
            await executor.ExecuteAsync(executor.Command<RenderQuestionPostCommand>(), request.Question);
        }

        if (request.Home != null)
            await executor.ExecuteAsync(executor.Command<RenderHomeTabCommand>(), request.Home);
    }
    
    public object Any(PreviewMarkdown request)
    {
        var html = markdown.GenerateHtml(request.Markdown);
        return html;
    }
}
