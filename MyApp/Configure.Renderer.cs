using Microsoft.AspNetCore.Components.Web;
using MyApp.Components.Pages;
using MyApp.Components.Shared;
using MyApp.Data;
using MyApp.ServiceInterface;
using MyApp.ServiceModel;
using ServiceStack.IO;

[assembly: HostingStartup(typeof(MyApp.ConfigureRenderer))]

namespace MyApp;

public class ConfigureRenderer : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services =>
        {
            services.AddScoped<HtmlRenderer>();
            services.AddScoped<BlazorRenderer>();
            services.AddSingleton<RendererCache>();
            services.RegisterService<RenderServices>();
        })
        .ConfigureAppHost(appHost => {
        });
}

public class RendererCache(AppConfig appConfig, R2VirtualFiles r2)
{
    private static bool DisableCache = false;
    
    public string GetCachedQuestionPostPath(int id) => appConfig.CacheDir.CombineWith(GetQuestionPostVirtualPath(id)); 
    public string GetQuestionPostVirtualPath(int id)
    {
        var idParts = id.ToFileParts();
        var fileName =  $"{idParts.fileId}.{nameof(QuestionPost)}.html";
        var dirPath = $"{idParts.dir1}/{idParts.dir2}";
        var filePath = $"{dirPath}/{fileName}";
        return filePath;
    }

    public async Task<string?> GetQuestionPostHtmlAsync(int id)
    {
        if (DisableCache)
            return null;
        var filePath = GetCachedQuestionPostPath(id);
        if (File.Exists(filePath))
            return await File.ReadAllTextAsync(filePath);
        return null;
    }

    public async Task SetQuestionPostHtmlAsync(int id, string html)
    {
        if (DisableCache)
            return;
        var (dir1, dir2, fileId) = id.ToFileParts();
        appConfig.CacheDir.CombineWith($"{dir1}/{dir2}").AssertDir();
        var filePath = GetCachedQuestionPostPath(id);
        await File.WriteAllTextAsync(filePath, html);
    }

    private string GetHtmlTabFilePath(string? tab)
    {
        var partialName = string.IsNullOrEmpty(tab) 
            ? "" 
            : $".{tab}";
        var filePath = appConfig.CacheDir.CombineWith($"HomeTab{partialName}.html");
        return filePath;
    }

    static TimeSpan HomeTabValidDuration = TimeSpan.FromMinutes(5);

    public async Task SetHomeTabHtmlAsync(string? tab, string html)
    {
        if (DisableCache)
            return;
        appConfig.CacheDir.AssertDir();
        var filePath = GetHtmlTabFilePath(tab);
        await File.WriteAllTextAsync(filePath, html);
    }

    public async Task<string?> GetHomeTabHtmlAsync(string? tab)
    {
        if (DisableCache)
            return null;
        var filePath = GetHtmlTabFilePath(tab);
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Exists)
        {
            if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc > HomeTabValidDuration)
                return null;
            
            var html = await fileInfo.ReadAllTextAsync();
            if (!string.IsNullOrEmpty(html))
                return html;
        }
        return null;
    }
}

public class RenderServices(R2VirtualFiles r2, BlazorRenderer renderer, RendererCache cache) : Service
{
    public async Task Any(RenderComponent request)
    {
        if (request.IfQuestionModified != null)
        {
            var id = request.IfQuestionModified.Value;
            var filePath = cache.GetCachedQuestionPostPath(id);
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                var questionFiles = await r2.GetQuestionFilesAsync(id);
                if (questionFiles.Files.FirstOrDefault()?.LastModified > fileInfo.LastWriteTime)
                {
                    request.Question = await questionFiles.ToQuestionAndAnswers();
                }
            }
        }
        
        if (request.Question != null)
        {
            var html = await renderer.RenderComponent<QuestionPost>(new() { ["Question"] = request.Question });
            await cache.SetQuestionPostHtmlAsync(request.Question.Id, html);
        }

        if (request.Home != null)
        {
            var html = await renderer.RenderComponent<HomeTab>(new()
            {
                ["Tab"] = request.Home.Tab,
                ["Posts"] = request.Home.Posts,
            });
            await cache.SetHomeTabHtmlAsync(request.Home.Tab, html);
        }
    }
}
