﻿using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Text;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Questions)]
[Worker(Databases.App)]
public class ImportQuestionCommand(ILogger<ImportQuestionCommand> log, AppConfig appConfig) : AsyncCommand<ImportQuestion>
{
    static readonly Regex ValidTagCharsRegex = new("[^a-zA-Z0-9#+.]", RegexOptions.Compiled);
    static readonly Regex SingleWhiteSpaceRegex = new(@"\s+", RegexOptions.Multiline | RegexOptions.Compiled);

    public HashSet<string> IgnoreTags { get; set; } =
    [
        "this", "was", "feedback", "this"
    ];
    
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

    protected override async Task RunAsync(ImportQuestion request, CancellationToken token)
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
            var json = await url.GetJsonFromUrlAsync(token: token);
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
                    }, token: token);
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
                    }, token: token);
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

    private async Task<string> GetJsonFromRedditAsync(string url)
    {
        url = url.Replace("www.reddit.com", "oauth.reddit.com");
        if (appConfig.RedditAccessToken != null)
        {
            try
            {
                return await url.GetJsonFromUrlAsync(requestFilter: req => {
                    req.AddBearerToken(appConfig.RedditAccessToken);
                    req.AddHeader("User-Agent", "pvq.app");
                });
            }
            catch (Exception e)
            {
                log.LogWarning("Failed to fetch Reddit API: {Message}\nRetrieving new access_token...", e.Message);
                appConfig.RedditAccessToken = null;
            }
        }

        appConfig.RedditAccessToken = await FetchNewRedditAccessTokenAsync();
        
        var json = await url.GetJsonFromUrlAsync(requestFilter: req => {
            req.AddBearerToken(appConfig.RedditAccessToken);
            req.AddHeader("User-Agent", "pvq.app");
        });
        return json;
    }

    private async Task<string> FetchNewRedditAccessTokenAsync()
    {
        Dictionary<string, object> postData = new()
        {
            ["grant_type"] = "client_credentials",
            ["device_id"] = Guid.NewGuid().ToString("N"),
        };
        var response = await "https://www.reddit.com/api/v1/access_token".PostToUrlAsync(postData, requestFilter: req =>
        {
            req.AddBasicAuth(
                appConfig.RedditClient ?? throw new ArgumentNullException(nameof(appConfig.RedditClient)), 
                appConfig.RedditSecret ?? throw new ArgumentNullException(nameof(appConfig.RedditSecret))); 
            req.AddHeader("User-Agent", "pvq.app");
        });
        var obj = (Dictionary<string,object>)JSON.parse(response);
        return (string)obj["access_token"];
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