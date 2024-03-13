using ServiceStack.IO;

namespace MyApp;

public class MarkdownQuestions(ILogger<MarkdownPages> log, IWebHostEnvironment env, IVirtualFiles fs)
    : MarkdownPagesBase<MarkdownFileInfo>(log, env, fs)
{
    public override string Id => "questions";

    public string GetDateLabel(DateTime? date) => X.Map(date ?? DateTime.UtcNow, d => d.ToString("MMMM d, yyyy"))!;
    public string GetDateTimestamp(DateTime? date) => X.Map(date ?? DateTime.UtcNow, d => d.ToString("O"))!;

    public string GenerateHtml(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        markdown = markdown.Replace("```", "\n```"); //fix for starcoder2:3b
        var writer = new StringWriter();
        CreateMarkdownFile(markdown, writer);
        var html = writer.ToString();
        return html;
    }
}
