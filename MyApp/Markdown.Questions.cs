using ServiceStack.IO;

namespace MyApp;

public class MarkdownQuestions(ILogger<MarkdownQuestions> log, IWebHostEnvironment env, IVirtualFiles fs)
    : MarkdownPagesBase<MarkdownFileInfo>(log, env, fs)
{
    public override string Id => "questions";

    public string GetDateLabel(DateTimeOffset dateTimeOffset) => GetDateLabel(dateTimeOffset.DateTime);
    public string GetDateLabel(DateTime? date) => GetDateLabel(date ?? DateTime.UtcNow);
    public string GetDateLabel(DateTime date) => date.ToString("MMM d 'at' HH:mm");
    public string GetDateTimestamp(DateTime? date) => X.Map(date ?? DateTime.UtcNow, d => d.ToString("O"))!;

    public string GenerateHtml(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        // markdown = markdown.Replace("```", "\n```"); //fix for starcoder2:3b
        var writer = new StringWriter();
        CreateMarkdownFile(markdown, writer);
        var html = writer.ToString();
        return html;
    }

    public string GenerateCommentHtml(string? markdown)
    {
        // extra processing for comments
        return GenerateHtml(markdown);
    }
}
