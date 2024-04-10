using System.Text.RegularExpressions;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;

namespace MyApp.ServiceInterface.App;

public class ImportQuestionCommand(AppConfig appConfig) : IAsyncCommand<ImportQuestion>
{
    static readonly Regex ValidTagCharsRegex = new("[^a-zA-Z0-9#+.]", RegexOptions.Compiled);
    static readonly Regex SingleWhiteSpaceRegex = new(@"\s+", RegexOptions.Multiline | RegexOptions.Compiled);
    
    public Dictionary<string, string> TagAliases { get; set; } = new()
    {
        ["ai"] = "artificial-intelligence",
        ["llm"] = "large-language-models",
    };

    public AskQuestion? Result { get; set; }

    public async Task ExecuteAsync(ImportQuestion request)
    {
        if (string.IsNullOrEmpty(request.Url))
            throw new ArgumentNullException(nameof(request.Url));
        
        if (request.Site == ImportSite.Unknown)
            request.Site = InferSiteFromUrl(request.Url);
        
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
            Result = new()
            {
                Title = (string)obj["title"],
                Body = body,
                Tags = ExtractTags(body),
            };
        }
        else throw new NotSupportedException("Unsupported Site");

        if (Result == null)
            throw new Exception("Import failed");
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

    public List<string> ExtractTags(string text, int count=5)
    {
        var prepareWords = ValidTagCharsRegex.Replace(text, " ");
        prepareWords = SingleWhiteSpaceRegex.Replace(prepareWords, " ").Trim();

        var tags = new List<string>();

        var words = prepareWords.Split();

        // var sortedWords = words.ToSet().OrderBy(x => x).ToList();
        // sortedWords.ForEach(Console.WriteLine);
        
        foreach (var word in words)
        {
            if (tags.Count >= count)
                break;
            
            var candidate = word.TrimEnd('.');
            candidate = candidate.ToKebabCase();
            if (TagAliases.TryGetValue(candidate, out var alias))
            {
                tags.Add(alias);
                continue;
            }
            if (appConfig.AllTags.Contains(candidate))
            {
                tags.Add(candidate);
                continue;
            }
            candidate = candidate.Replace("-", "");
            if (appConfig.AllTags.Contains(candidate))
            {
                tags.Add(candidate);
                continue;
            }
        }

        return tags;
    }
}