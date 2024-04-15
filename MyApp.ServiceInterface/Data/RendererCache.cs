using ServiceStack;
using ServiceStack.IO;

namespace MyApp.Data;

public class RendererCache(AppConfig appConfig, R2VirtualFiles r2)
{
    private static bool DisableCache = false;

    public string GetCachedQuestionPostPath(int id) => appConfig.CacheDir.CombineWith(GetQuestionPostVirtualPath(id));

    public string GetQuestionPostVirtualPath(int id)
    {
        var idParts = id.ToFileParts();
        var fileName = $"{idParts.fileId}.QuestionPost.html";
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

    public void DeleteCachedQuestionPostHtml(int id)
    {
        try
        {
            File.Delete(GetCachedQuestionPostPath(id));
        }
        catch {}
    }
    
    public async Task SetQuestionPostHtmlAsync(int id, string? html)
    {
        if (DisableCache)
            return;
        if (!string.IsNullOrEmpty(html))
            throw new ArgumentNullException(html);
        
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

    public void DeleteHomeTabHtml(string? tab)
    {
        try
        {
            var filePath = GetHtmlTabFilePath(tab);
            var fileInfo = new FileInfo(filePath);
            fileInfo.Delete();
        }
        catch {}
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