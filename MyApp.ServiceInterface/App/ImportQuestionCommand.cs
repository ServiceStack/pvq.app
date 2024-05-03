using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Text;

namespace MyApp.ServiceInterface.App;

public class ImportQuestionCommand(ILogger<ImportQuestionCommand> log, AppConfig appConfig) : IAsyncCommand<ImportQuestion>
{
    static readonly Regex ValidTagCharsRegex = new("[^a-zA-Z0-9#+.]", RegexOptions.Compiled);
    static readonly Regex SingleWhiteSpaceRegex = new(@"\s+", RegexOptions.Multiline | RegexOptions.Compiled);

    public HashSet<string> IgnoreTags { get; set; } = new()
    {
        "this", "was", "feedback", "this", 
    };
    
    public Dictionary<string, string> TagAliases { get; set; } = new()
    {
        ["dotnet"] = ".net",
        ["csharp"] = "c#",
        ["fsharp"] = "f#",
        ["cpp"] = "c++",
        ["ai"] = "artificial-intelligence",
        ["llm"] = "large-language-models",
    };

    public AskQuestion? Result { get; set; }

    public async Task ExecuteAsync(ImportQuestion request)
    {
        if (string.IsNullOrEmpty(request.Url))
            throw new ArgumentNullException(nameof(request.Url));

        if (!Uri.IsWellFormedUriString(request.Url, UriKind.Absolute))
            throw new ArgumentException("Invalid URL", nameof(request.Url));
        
        if (request.Site == ImportSite.Unknown)
            request.Site = InferSiteFromUrl(request.Url);
        
        var uri = new Uri(request.Url);
        
        log.LogInformation("Importing new question from {Site} URL: {Url}", request.Site, uri);

        if (request.Site == ImportSite.Discourse)
        {
            var url = request.Url.LeftPart('?');
            
            url += ".json?include_raw=true";
            var json = await url.GetJsonFromUrlAsync();
            var obj = (Dictionary<string, object>)JSON.parse(json);

            if (!(obj.TryGetValue("post_stream", out var oPostStream) &&
                  oPostStream is Dictionary<string, object> postStream))
                throw new NotSupportedException("post_stream not found");
            if (!(postStream.TryGetValue("posts", out var oPosts) && oPosts is List<object> posts))
                throw new NotSupportedException("posts not found");

            var postObj = (Dictionary<string, object>)posts[0];
            var body = (string)postObj["raw"];
            var tags = request.Tags ?? [];
            tags.AddRange(ExtractTags(body, count:5 - tags.Count));

            var parts = uri.AbsolutePath.Trim('/').Split('/');
            var postId = parts.Length >= 3 && int.TryParse(parts[2], out var id) ? id : 0;
            
            Result = new()
            {
                Title = (string)obj["title"],
                Body = body.Trim(),
                Tags = ExtractTags(body),
                RefUrn = postId > 0 ? $"{uri.Host}:{postId}" : null,
            };
        }
        else if (request.Site == ImportSite.StackOverflow)
        {
            var parts = uri.AbsolutePath.Trim('/').Split('/');
            if (parts.Length >= 2 && (parts[0] == "q" || parts[0] == "questions") && int.TryParse(parts[1], out var postId))
            {
                try
                {
                    var site = uri.Host.LeftPart('.');
                    var apiUrl = $"https://api.stackexchange.com/2.3/questions/{postId}?filter=!9YdnSJ*_T&site={site}";
                    var json = await apiUrl.GetJsonFromUrlAsync(requestFilter: c => {
                        c.AddHeader(HttpHeaders.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:83.0) Gecko/20100101 Firefox/83.0");
                        c.AddHeader(HttpHeaders.Accept, MimeTypes.Json);
                    });
                    var obj = (Dictionary<string, object>)JSON.parse(json);
                    if (obj.TryGetValue("items", out var oItems) && oItems is List<object> items)
                    {
                        if (items.FirstOrDefault() is Dictionary<string, object> post)
                        {
                            var title = (string)post["title"];
                            var body = (string)post["body"];
                            var tags = ((List<object>)post["tags"]).Cast<string>().ToList();

                            Result = new()
                            {
                                Title = title,
                                Body = body.Trim(),
                                Tags = tags,
                            };
                        }
                    }
                }
                catch (Exception e)
                {
                    log.LogWarning("Failed to fetch StackOverflow API: {Message}\nTrying HTML...", e.Message);
                }

                if (Result == null)
                {
                    var htmlUrl = $"{uri.Scheme}://{uri.Host}/posts/{postId}/edit-inline";
                    var html = await htmlUrl.GetStringFromUrlAsync(requestFilter: c =>
                    {
                        c.AddHeader(HttpHeaders.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:83.0) Gecko/20100101 Firefox/83.0");
                        c.AddHeader(HttpHeaders.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                        c.AddHeader(HttpHeaders.AcceptLanguage, "en-US,en;q=0.9");
                        c.AddHeader(HttpHeaders.CacheControl, "max-age=0");
                    });
                    Result = CreateFromStackOverflowInlineEdit(html);
                }
                
                if (Result != null)
                {
                    Result.RefUrn = $"{uri.Host}:{postId}";
                }
            }
        }
        else if (request.Site == ImportSite.Reddit)
        {
            var url = request.Url.Trim('/') + ".json";
            var json = await GetJsonFromRedditAsync(url);
            var objs = (List<object>)JSON.parse(json);
            var obj = (Dictionary<string, object>)objs[0];
            var data = (Dictionary<string, object>)obj["data"];
            var children = (List<object>)data["children"];
            var post = (Dictionary<string, object>)children[0];
            var postData = (Dictionary<string, object>)post["data"];
            var id = (string)postData["id"];
            var subreddit = (string)postData["subreddit"];
            var title = (string)postData["title"];
            var body = (string)postData["selftext"];
            var tags = request.Tags ?? [];
            
            var subredditTag = GetMatchingTag(subreddit);
            if (subredditTag != null)
                tags.Add(subredditTag);
            
            tags.AddRange(ExtractTags(body, count:5 - tags.Count));
            
            Result = new()
            {
                Title = title.Trim(),
                Body = body.HtmlDecode().Trim(),
                Tags = tags,
                RefUrn = $"reddit.{subreddit}:{id}",
            };
        }
        else throw new NotSupportedException("Unsupported Site");

        if (Result == null)
            throw new Exception("Import failed");
    }

    private static async Task<string> GetJsonFromRedditAsync(string url)
    {
        // C# HttpClient requests are getting blocked
        // var json = await url.GetJsonFromUrlAsync(requestFilter: c =>
        // {
        //     c.AddHeader(HttpHeaders.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:83.0) Gecko/20100101 Firefox/83.0");
        //     c.AddHeader(HttpHeaders.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        //     c.AddHeader(HttpHeaders.AcceptLanguage, "en-US,en;q=0.9");
        //     c.AddHeader(HttpHeaders.CacheControl, "max-age=0");
        // });
        // return json;

        // Using curl Instead:
        var args = new[]
        {
            $"curl -s '{url}'",
            "-H 'accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7'",
            "-H 'accept-language: en-US,en;q=0.9'",
            "-H 'cache-control: max-age=0'",
            "-H 'dnt: 1'",
            "-H 'priority: u=0, i'",
            "-H 'upgrade-insecure-requests: 1'",
            "-H 'user-agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36'"
        }.ToList();
        if (Env.IsWindows)
        {
            args = args.Map(x => x.Replace('\'', '"'));
        }
        
        var argsString = string.Join(" ", args);
        var sb = StringBuilderCache.Allocate();
        await ProcessUtils.RunShellAsync(argsString, onOut:line => sb.AppendLine(line));
        var json = StringBuilderCache.ReturnAndFree(sb);
        return json;
    }

    public static AskQuestion? CreateFromStackOverflowInlineEdit(string html)
    {
        var span = html.AsSpan();

        const string titleStart1 = "<input id=\"title\"";
        const string titleStart2 = "value=\"";
        span = span.Advance(span.IndexOf(titleStart1) + titleStart1.Length);
        span = span.Advance(span.IndexOf(titleStart2) + titleStart2.Length);
        
        var title = span[..span.IndexOf('"')].ToString().HtmlDecode();
        
        const string bodyStart = "<textarea ";
        span = span.Advance(span.IndexOf(bodyStart) + bodyStart.Length);
        span = span.AdvancePastChar('>');
        
        var body = span[..span.IndexOf('<')].ToString().HtmlDecode();

        const string tagsStart1 = "<input id=\"tagnames\"";
        const string tagsStart2 = "value=\"";
        span = span.Advance(span.IndexOf(tagsStart1) + tagsStart1.Length);
        span = span.Advance(span.IndexOf(tagsStart2) + tagsStart2.Length);

        var tagsValue = span[..span.IndexOf('"')].ToString().HtmlDecode();
        var tags = tagsValue.Split(' ');

        var to = new AskQuestion
        {
            Title = title,
            Body = body.Trim(),
            Tags = [..tags],
        };

        return to;
    }

    private ImportSite InferSiteFromUrl(string requestUrl)
    {
        if (requestUrl.Contains("stackoverflow.com"))
            return ImportSite.StackOverflow;
        if (requestUrl.Contains("reddit.com"))
            return ImportSite.Reddit;
        if (requestUrl.Contains("/t/"))
            return ImportSite.Discourse;
        
        throw new NotSupportedException("Unsupported Site");
    }

    string? GetMatchingTag(string candidate)
    {
        if (candidate.Length <= 1)
            return null;
        candidate = candidate.ToKebabCase();
        if (!IgnoreTags.Contains(candidate) && TagAliases.TryGetValue(candidate, out var alias))
            return alias;
        if (!IgnoreTags.Contains(candidate) && appConfig.AllTags.Contains(candidate))
            return candidate;
        candidate = candidate.Replace("-", "");
        if (candidate.Length <= 1)
            return null;
        if (!IgnoreTags.Contains(candidate) && appConfig.AllTags.Contains(candidate))
            return candidate;
        return null;
    }

    public List<string> ExtractTags(string text, int count=5)
    {
        var tags = new List<string>();
        
        foreach (var line in text.ReadLines())
        {
            if (line.StartsWith("```"))
            {
                var lang = line[3..].Trim();
                var tag = GetMatchingTag(lang);

                if (tag != null && !tags.Contains(tag))
                {
                    tags.Add(tag);
                    if (tags.Count >= count)
                        break;
                }
            }
        }
        
        var prepareWords = ValidTagCharsRegex.Replace(text, " ");
        prepareWords = SingleWhiteSpaceRegex.Replace(prepareWords, " ").Trim();

        var words = prepareWords.Split();

        // var sortedWords = words.ToSet().OrderBy(x => x).ToList();
        // sortedWords.ForEach(Console.WriteLine);
        
        foreach (var word in words)
        {
            var candidate = word.TrimEnd('.');
            var tag = GetMatchingTag(candidate);

            if (tag != null && !tags.Contains(tag))
            {
                tags.Add(tag);
                if (tags.Count >= count)
                    break;
            }
        }

        return tags;
    }
}