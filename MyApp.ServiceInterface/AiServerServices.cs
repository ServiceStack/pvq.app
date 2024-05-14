using System.Text.RegularExpressions;
using AiServer.ServiceModel;
using Markdig.Helpers;
using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceInterface.AiServer;
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
    public async Task<object> Any(CreateAnswersForModels request)
    {
        if (request.Models.IsEmpty())
            throw new ArgumentNullException(nameof(request.Models));
        
        var command = executor.Command<CreateAnswerTasksCommand>();
        var to = new CreateAnswersForModelsResponse();

        foreach (var postId in request.PostIds)
        {
            var post = await questions.GetQuestionFileAsPostAsync(postId);
            if (post == null)
            {
                to.Errors[postId] = "Missing QuestionFile";
                continue;
            }
            await command.ExecuteAsync(new CreateAnswerTasks
            {
                Post = post,
                ModelUsers = request.Models,
            });
            to.Results.Add(post.Id);
        }
        return to;
    }

    public async Task<object> Any(CreateRankingTasks request)
    {
        var command = executor.Command<CreateRankAnswerTaskCommand>();

        var to = new CreateRankingTasksResponse();

        var uniqueUserNames = request.AnswerIds.Select(x => x.RightPart('-')).ToSet();
        var userIdMap = await Db.DictionaryAsync<string, string>(
            Db.From<ApplicationUser>().Where(x => uniqueUserNames.Contains(x.UserName!))
                .Select(x => new { x.UserName, x.Id }));
        
        foreach (var id in request.AnswerIds)
        {
            try
            {
                var postId = id.LeftPart('-').ToInt();
                var userName = id.RightPart('-');
                if (!userIdMap.TryGetValue(userName, out var userId))
                {
                    to.Errors[id] = "Unknown User";
                    continue;
                }
                await command.ExecuteAsync(new CreateRankAnswerTask
                {
                    UserId = userId,
                    AnswerId = id,
                });
            }
            catch (Exception e)
            {
                to.Errors[id] = e.Message;
            }
        }
        return to;
    }

    public async Task Any(CreateAnswerCallback request)
    {
        var modelUser = appConfig.GetModelUserById(request.UserId);
        if (modelUser?.UserName == null)
            throw HttpError.BadRequest("Invalid Model User Id");

        rendererCache.DeleteCachedQuestionPostHtml(request.PostId);

        var answer = request.ToAnswer(request.PostId, modelUser.UserName);
        
        await questions.SaveAnswerAsync(answer);

        MessageProducer.Publish(new DbWrites
        {
            SaveStartingUpVotes = new()
            {
                Id = answer.RefId!,
                PostId = request.PostId,
                StartingUpVotes = 0,
                CreatedBy = modelUser.UserName,
                LastUpdated = DateTime.UtcNow,
            }
        });

        await Db.NotifyQuestionAuthorIfRequiredAsync(MessageProducer, answer);
        
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
        var answerCreator = await AssertUserNameById(request.UserId);

        var graderUser = appConfig.GetModelUser(request.Grader);
        if (graderUser?.UserName == null)
            throw HttpError.BadRequest("Invalid Model Grader: " + request.Model);

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
            meta.GradedBy[answerCreator] = graderUser.UserName;
                
            meta.ModelReasons ??= new();
            meta.ModelReasons[answerCreator] = reason ?? "";
                
            meta.ModelVotes ??= new();
            meta.ModelVotes[answerCreator] = score;

            var statTotals = new StatTotals
            {
                Id = $"{request.PostId}-{answerCreator}",
                PostId = request.PostId,
                StartingUpVotes = score,
                CreatedBy = answerCreator,
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

    public async Task Any(AnswerCommentCallback request)
    {
        var commentCreator = await AssertUserNameById(request.UserId);
        var postId = request.AnswerId.LeftPart('-').ToInt();
        var modelUserName = request.AnswerId.RightPart('-');

        var modelUser = appConfig.GetModelUser(modelUserName);
        if (modelUser?.UserName == null)
            throw HttpError.BadRequest("Invalid Model: " + modelUserName);

        var metaFile = await questions.GetMetaFileAsync(postId);
        if (metaFile == null)
            throw HttpError.BadRequest("Invalid Post: " + postId);

        var metaJson = await metaFile.ReadAllTextAsync();
        var meta = metaJson.FromJson<Meta>();

        var comments = meta.Comments.GetOrAdd(request.AnswerId, key => new());

        var body = request.Choices?.FirstOrDefault()?.Message?.Content.GenerateModelComment();
        if (string.IsNullOrEmpty(body))
        {
            log.LogError("Invalid AnswerCommentCallback: {AnswerId} for {UserId}: body missing", 
                request.AnswerId, request.UserId);
            return;
        }

        var commentBody = $"@{commentCreator} {body}";
        var newComment = new Comment
        {
            Body = commentBody,
            Created = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            CreatedBy = modelUser.UserName,
            AiRef = request.AiRef,
        };
        comments.Add(newComment);

        MessageProducer.Publish(new DbWrites
        {
            NewComment = new()
            {
                RefId = request.AnswerId,
                Comment = newComment,
                LastUpdated = DateTime.UtcNow,
            },
        });

        await questions.SaveMetaAsync(postId, meta);
    }

    private async Task<string> AssertUserNameById(string userId)
    {
        var userName = appConfig.GetModelUserById(userId)?.UserName
            ?? await Db.ScalarAsync<string?>(Db.From<ApplicationUser>().Where(x => x.Id == userId).Select(x => x.UserName));
        if (userName == null)
            throw HttpError.BadRequest("Invalid User Id: " + userId);
        return userName;
    }
}

public static class AiServerExtensions
{
    static readonly Regex StripHtmlRegEx = new(@"<[^>]*>?", RegexOptions.Compiled | RegexOptions.Multiline);
    static readonly Regex StripCodeBlocks = new(@"```[^`]+```", RegexOptions.Compiled | RegexOptions.Multiline);
    static readonly Regex CollapseNewLines = new(@"[\r\n]+", RegexOptions.Compiled | RegexOptions.Multiline);
    static readonly Regex CollapseSpaces = new(@"\s+", RegexOptions.Compiled | RegexOptions.Multiline);

    public static string GenerateNotificationTitle(this string title) => title.SubstringWithEllipsis(0, 100);

    public static string GenerateNotificationSummary(this string summary, int startPos=0) => 
        summary.SubstringWithEllipsis(startPos, 100);
    
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

    public static string GenerateComment(this string body)
    {
        // body = body.Replace("\r\n", " ").Replace('\n', ' ');
        return body;
    }

    public static string GenerateModelComment(this string body)
    {
        // body = body.TrimStart('#').Replace("\r\n", " ").Replace('\n', ' ');
        return body;
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
