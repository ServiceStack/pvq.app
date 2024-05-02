using System.Text.RegularExpressions;
using AiServer.ServiceModel;
using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class AiServerServices(ILogger<AiServerServices> log,
    AppConfig appConfig, 
    QuestionsProvider questions, 
    RendererCache rendererCache, 
    WorkerAnswerNotifier answerNotifier,
    ICommandExecutor executor) : Service
{
    public async Task Any(CreateAnswerCallback request)
    {
        if (request.PostId == 0)
            request.PostId = int.TryParse(Request!.QueryString[nameof(request.PostId)], out var postId)
                ? postId
                : throw new ArgumentNullException(nameof(request.PostId));
        if (string.IsNullOrEmpty(request.UserId))
            request.UserId = Request!.QueryString[nameof(request.UserId)] ?? throw new ArgumentNullException(nameof(request.UserId));
        
        var modelUser = appConfig.GetModelUserById(request.UserId);
        if (modelUser?.UserName == null)
            throw HttpError.Forbidden("Invalid Model User Id");

        rendererCache.DeleteCachedQuestionPostHtml(request.PostId);

        var answer = request.ToAnswer(request.PostId, modelUser.UserName);
        
        await questions.SaveHumanAnswerAsync(answer);
            
        MessageProducer.Publish(new DbWrites
        {
            SaveStartingUpVotes = new()
            {
                Id = answer.RefId!,
                PostId = request.PostId,
                StartingUpVotes = 0,
                CreatedBy = modelUser.UserName,
            }
        });

        // Only add notifications for answers older than 1hr
        var post = await Db.SingleByIdAsync<Post>(request.PostId);
        if (post?.CreatedBy != null && DateTime.UtcNow - post.CreationDate > TimeSpan.FromHours(1))
        {
            if (!string.IsNullOrEmpty(answer.Summary))
            {
                MessageProducer.Publish(new DbWrites {
                    CreateNotification = new()
                    {
                        UserName = post.CreatedBy,
                        PostId = post.Id,
                        Type = NotificationType.NewAnswer,
                        CreatedDate = DateTime.UtcNow,
                        RefId = answer.RefId!,
                        Summary = answer.Summary,
                        RefUserName = answer.CreatedBy,
                    },
                });
            }
        }
        
        MessageProducer.Publish(new AiServerTasks
        {
            CreateRankAnswerTask = new CreateRankAnswerTask {
                AnswerId = answer.RefId!,
                UserId = request.UserId,
            } 
        });
        
        MessageProducer.Publish(new SearchTasks {
            AddAnswerToIndex = answer.RefId
        });
    }
    
    public async Task Any(RankAnswerCallback request)
    {
        if (request.PostId == 0)
            request.PostId = int.TryParse(Request!.QueryString[nameof(request.PostId)], out var postId)
                ? postId
                : throw new ArgumentNullException(nameof(request.PostId));
        if (string.IsNullOrEmpty(request.UserId))
            request.UserId = Request!.QueryString[nameof(request.UserId)] ?? throw new ArgumentNullException(nameof(request.UserId));

        if (string.IsNullOrEmpty(request.Grader))
            request.Grader = Request!.QueryString[nameof(request.Grader)] ?? throw new ArgumentNullException(nameof(request.Grader));
        
        var modelUser = appConfig.GetModelUserById(request.UserId);
        if (modelUser?.UserName == null)
            throw HttpError.Forbidden("Invalid Model User Id");

        var graderUser = appConfig.GetModelUser(request.Grader);
        if (graderUser?.UserName == null)
            throw HttpError.Forbidden("Invalid Model Grader " + request.Model);

        var body = request.GetBody()?.Trim();
        if (string.IsNullOrEmpty(body))
        {
            log.LogError("Invalid RankAnswerCallback: {PostId}-{Model} for {UserId}", 
                request.PostId, request.Model, request.UserId);
            return;
        }

        try
        {
            var rankResponse = body.ParseRankResponse();
            if (rankResponse == null)
            {
                log.LogError("Invalid RankAnswerCallback: {PostId}-{Model} for {UserId}: {Body}", 
                    request.PostId, request.Model, request.UserId, body);
                return;
            }

            var (reason, score) = rankResponse;
            var meta = await questions.GetMetaAsync(request.PostId);
                
            meta.GradedBy ??= new();
            meta.GradedBy[modelUser.UserName] = graderUser.UserName;
                
            meta.ModelReasons ??= new();
            meta.ModelReasons[modelUser.UserName] = reason ?? "";
                
            meta.ModelVotes ??= new();
            meta.ModelVotes[modelUser.UserName] = score;

            var statTotals = new StatTotals
            {
                Id = $"{request.PostId}-{modelUser.UserName}",
                PostId = request.PostId,
                StartingUpVotes = score,
                CreatedBy = modelUser.UserName,
                LastUpdated = DateTime.UtcNow,
            };

            meta.StatTotals ??= new();
            meta.StatTotals.RemoveAll(x => x.Id == statTotals.Id);
            meta.StatTotals.Add(statTotals);

            await questions.SaveMetaAsync(request.PostId, meta);
            
            MessageProducer.Publish(new DbWrites
            {
                SaveStartingUpVotes = statTotals 
            });
        }
        catch (Exception e)
        {
            log.LogError(e, "Invalid JSON RankAnswerCallback: {PostId}-{Model} for {UserId}: {Json}", 
                request.PostId, request.Model, request.UserId, body);
        }
    }
}

public static class AiServerExtensions
{
    static readonly Regex StripHtmlRegEx = new(@"<[^>]*>?", RegexOptions.Compiled | RegexOptions.Multiline);
    static readonly Regex StripCodeBlocks = new(@"```[^`]+```", RegexOptions.Compiled | RegexOptions.Multiline);
    static readonly Regex CollapseNewLines = new(@"[\r\n]+", RegexOptions.Compiled | RegexOptions.Multiline);
    static readonly Regex CollapseSpaces = new(@"\s+", RegexOptions.Compiled | RegexOptions.Multiline);

    public static string GenerateSummary(this string body)
    {
        string withoutHtml = StripHtmlRegEx.Replace(body, string.Empty); // naive html stripping
        string withoutCode = StripCodeBlocks.Replace(withoutHtml, string.Empty); // remove code blocks
        string summary = CollapseNewLines.Replace(withoutCode, " ").Replace("  ", " ").Trim(); // collapse new lines and spaces

        if (summary.Length < 20)
        {
            summary = Regex.Replace(withoutHtml, @"```", " ");
            summary = CollapseNewLines.Replace(summary, " ");
            summary = CollapseSpaces.Replace(summary, " ").Trim();
        }

        summary = summary.Length > 200 ? summary.Substring(0, 200) + "..." : summary;
        return summary;
    }

    public static string? GetBody(this OpenAiChatResponse request) => request.Choices?.FirstOrDefault()?.Message?.Content;
    
    public static Post ToAnswer(this OpenAiChatResponse request, int postId, string userName)
    {
        var body = SanitizeBody(request.GetBody());
        var to = new Post
        {
            ParentId = postId,
            PostTypeId = 2,
            Summary = body?.GenerateSummary() ?? "",
            CreatedBy = userName,
            CreationDate = DateTime.UtcNow,
            Body = body,
            RefId = $"{postId}-{userName}"
        };
        return to;
    }

    public static Dictionary<string, string> ReplaceBodyTokens = new()
    {
        ["```C#"] = "```csharp",
    };

    public static string SanitizeBody(string? body)
    {
        if (string.IsNullOrEmpty(body))
            return string.Empty;
        
        foreach (var (from, to) in ReplaceBodyTokens)
        {
            if (body.IndexOf(from, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                body = body.Replace(from, to);
            }
        }
        return body;
    }

    public static GradeResult? ParseRankResponse(this string body)
    {
        var json = body.Contains("```json")
            ? body.RightPart("```json").LastLeftPart("```")
            : body.StartsWith("{")
                ? body
                : null;
        
        if (json == null)
        {
            var reasonPos = body.IndexOf("\"reason\"", StringComparison.Ordinal);
            if (reasonPos >= 0)
            {
                var lastPos = body.LastIndexOf('}');
                if (lastPos >= 0)
                {
                    json = string.Concat("{", body.AsSpan(reasonPos, lastPos - reasonPos + 1));
                }
            }
        }

        if (!string.IsNullOrEmpty(json))
        {
            var obj = (Dictionary<string,object>)JSON.parse(json);
            var reason = obj.TryGetValue("reason", out var oReason)
                ? (string)oReason
                : null;
            if (reason == null)
                return null;
            // When score is invalid (e.g. N/A), default to 0
            var score = obj.TryGetValue("score", out var oScore)
                ? oScore.ConvertTo<int>()
                : 0;

            return new() { Reason = reason, Score = score };
        }
        return null;
    }
}
