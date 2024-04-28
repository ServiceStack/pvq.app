using System.Text.RegularExpressions;
using AiServer.ServiceModel;
using MyApp.Data;
using MyApp.ServiceInterface.AiServer;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class AiServerServices(AppConfig appConfig, 
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
        
        answerNotifier.NotifyNewAnswer(request.PostId, modelUser.UserName);

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
                AnswerId = answer.RefId!
            } 
        });
    }
    
    public async Task Any(RankAnswerCallback request)
    {
        
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
        var body = request.GetBody();
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
}
