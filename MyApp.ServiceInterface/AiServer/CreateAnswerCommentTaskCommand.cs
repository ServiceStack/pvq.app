using AiServer.ServiceModel;
using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Jobs;

namespace MyApp.ServiceInterface.AiServer;

public class CreateAnswerCommentTask
{
    public string? AiRef { get; set; }
    public string Model { get; set; }
    public Post Question { get; set; }
    public Post Answer { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public List<Comment> Comments { get; set; }
}

[Tag(Tags.AI)]
public class CreateAnswerCommentTaskCommand(ILogger<CreateAnswerCommentTaskCommand> logger, 
    IBackgroundJobs jobs, AppConfig appConfig) : AsyncCommand<CreateAnswerCommentTask>
{
    public const string SystemPrompt = 
        """
        You are an IT expert helping a user with a technical issue using your computer science, network infrastructure, 
        and IT security knowledge to solve my problem using data from StackOverflow, Hacker News, and GitHub of content 
        like issues submitted, closed issues, number of stars on a repository, and overall StackOverflow activity.
        I will provide you with my original question and your initial answer attempt to solve my problem and 
        my follow up questions asking for further explanation and clarification of your answer. 
        You should use your expertise to provide specific, concise answers to my follow up questions.
        """;

    protected override async Task RunAsync(CreateAnswerCommentTask request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        var question = request.Question;

        request.AiRef ??= Guid.NewGuid().ToString("N");
        
        var answerPrompt = 
            $"""
            ## Original Answer Attempt

            {request.Answer.Body}
            ---
            """;

        var questionPrompt = CreateAnswerTasksCommand.CreateQuestionPrompt(question);
        var openAiChat = new OpenAiChat
        {
            Model = request.Model,
            Messages = [
                new() { Role = "system", Content = SystemPrompt },
                new() { Role = "user",   Content = questionPrompt },
                new() { Role = "assistant",  Content = answerPrompt },
            ],
            Temperature = 0.2,
            MaxTokens = 300,
        };

        var modelUser = appConfig.GetModelUser(request.Model);
        if (modelUser == null)
            throw new ArgumentException("Model User not found: " + request.Model);

        foreach (var comment in request.Comments)
        {
            if (comment.CreatedBy == request.UserName)
            {
                openAiChat.Messages.Add(new() { Role = "user", Content = comment.Body });
            }
            else if (comment.CreatedBy == modelUser.UserName)
            {
                openAiChat.Messages.Add(new() { Role = "assistant", Content = comment.Body });
            }
        }
        openAiChat.Messages.Add(new() { Role = "user", 
            Content = """
                      ## Instruction
                      Answer my follow up question above in a concise manner. 
                      Keep your response on the topic of the original question and answer, directly addressing my specific comment. 
                      Max 2-3 sentences.
                      """
        });

        var client = appConfig.CreateAiServerClient();
        var replyTo = appConfig.BaseUrl.CombineWith("api", nameof(AnswerCommentCallback).AddQueryParams(new()
        {
            [nameof(AnswerCommentCallback.AnswerId)] = request.Answer.RefId,
            [nameof(AnswerCommentCallback.UserId)] = request.UserId,
            [nameof(AnswerCommentCallback.AiRef)] = request.AiRef,
        }));
        
        var startedAt = DateTime.UtcNow;
        log.LogInformation("Sending CreateOpenAiChat for Question {Id} Answer Comment for {Model}, replyTo: {ReplyTo}", 
            question.Id, request.Model, replyTo);

        var api = await client.ApiAsync(new QueueOpenAiChatCompletion {
            RefId = request.AiRef,
            Tag = "pvq",
            Provider = null,
            ReplyTo = replyTo,
            Request = openAiChat
        });

        log.LogInformation("Completed CreateOpenAiChat for Question {Id} Answer Comment for {Model} in {Ms}: {Status}", 
            question.Id, request.Model, (int)(DateTime.UtcNow - startedAt).TotalMilliseconds, 
            api.Error == null ? "OK" : $"{api.Error.ErrorCode}: {api.Error.Message}");
        api.ThrowIfError();
    }
}
