using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Script;
using ServiceStack.Text;
using CreatorKit.ServiceInterface;
using CreatorKit.ServiceModel;

namespace CreatorKit;

public class EmailMarkdownScriptMethods : ScriptMethods
{
    public IRawString emailmarkdown(string? markdown) => markdown != null 
        ? EmailMarkdownScriptBlock.Transform(markdown).ToRawString() 
        : RawString.Empty;
}

/// <summary>
/// Converts markdown contents to HTML using the configured MarkdownConfig.Transformer.
/// If a variable name is specified the HTML output is captured and saved instead. 
///
/// Usages: {{#emailmarkdown}} ## The Heading {{/emailmarkdown}}
///         {{#emailmarkdown content}} ## The Heading {{/emailmarkdown}} HTML: {{content}}
/// </summary>
public class EmailMarkdownScriptBlock : ScriptBlock
{
    public override string Name => "emailmarkdown";

    public static string Prefix = "<div style=\"max-width:742px;margin:30px auto;font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI','Helvetica Neue',sans-serif\">";
    public static string Suffix = "</div>";

    public static Dictionary<string, string> ReplaceTokens { get; } = new()
    {
        ["<h1 "] = "<h1 style=\"color:rgb(51,51,51);font-weight:700;margin-top:0;margin-bottom:40px;font-size:34px;font-family:system-ui,-apple-system,BlinkMacSystemFont,Segoe UI,Helvetica Neue,Helvetica,Arial,sans-serif;line-height:1.1\" ",
        ["<h2 "] = "<h2 style=\"color:#333;font-size:28px;font-weight:600;margin-top:36px;margin-bottom:30px\" ",
        ["<h3 "] = "<h3 style=\"color:#333;font-size:22px;font-weight:600;margin-top:28px;margin-bottom:16px\" ",
        ["<h4 "] = "<h4 style=\"color:#333;font-size:18px;font-weight:600;margin-top:20px;margin-bottom:10px\" ",
        ["<h5 "] = "<h5 style=\"color:#333;font-size:16px;font-weight:700;margin-top:16px;margin-bottom:8px\" ",
        ["<h6 "] = "<h6 style=\"color:#333;font-size:16px;font-weight:normal;margin-top:10px;margin-bottom:4px\" ",
        ["<img "] = "<img style=\"max-width:100%\" ",
        ["<p>"] = "<p style=\"margin-bottom:1em;font-size:16px;color:#333333;line-height:1.5em;font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI','Helvetica Neue',sans-serif\">",
        ["<blockquote>"] = "<blockquote style=\"font-style:italic;color:#0f172a;margin:1.6em 0 1.6em 1em;padding:0 0 0 1em;\">",
        ["<ol>"] = "<ol style=\"list-style:decimal;margin:1em 0 0 1.6em;padding:0;\">",
        ["<ul>"] = "<ul style=\"list-style:disc;margin:1em 0 0 1.6em;padding:0;\">",
        ["<li>"] = "<li style=\"margin-bottom:1em;font-size:16px;color:#333333;line-height:1.5em;font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI','Helvetica Neue',sans-serif\">",
        ["<hr />"] = "<hr style=\"margin-top:40px;margin-bottom:40px;display:block;border:none;border-bottom:1px solid #e4e4e4\">",
    };

    public static string Transform(string markdown)
    {
        var html = MarkdownConfig.Transform(markdown);
        foreach (var entry in ReplaceTokens)
        {
            html = html.Replace(entry.Key, entry.Value);
        }
        return Prefix + html + Suffix;
    }
    
    public override async Task WriteAsync(
        ScriptScopeContext scope, PageBlockFragment block, CancellationToken token)
    {
        var strFragment = (PageStringFragment)block.Body[0];

        if (!block.Argument.IsNullOrWhiteSpace())
        {
            Capture(scope, block, strFragment);
        }
        else
        {
            await scope.OutputStream.WriteAsync(Transform(strFragment.ValueString), token);
        }
    }

    private static void Capture(
        ScriptScopeContext scope, PageBlockFragment block, PageStringFragment strFragment)
    {
        var literal = block.Argument.AdvancePastWhitespace();

        literal = literal.ParseVarName(out var name);
        var nameString = name.ToString();
        scope.PageResult.Args[nameString] = Transform(strFragment.ValueString).ToRawString();
    }
}