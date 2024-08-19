using AiServer.ServiceModel;
using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Jobs;

namespace MyApp.ServiceInterface.AiServer;

[Tag(Tags.AI)]
public class CreateAnswerTasksCommand(ILogger<CreateAnswerTasksCommand> logger, IBackgroundJobs jobs, AppConfig appConfig) 
    : AsyncCommand<CreateAnswerTasks>
{
    //https://github.com/f/awesome-chatgpt-prompts?tab=readme-ov-file#act-as-an-it-expert
    //https://github.com/f/awesome-chatgpt-prompts?tab=readme-ov-file#act-as-a-developer-relations-consultant
    public const string SystemPrompt = 
        """
        You are an IT expert helping a user with a technical issue.
        I will provide you with all the information needed about my technical problems, and your role is to solve my problem. 
        You should use your computer science, network infrastructure, and IT security knowledge to solve my problem
        using data from StackOverflow, Hacker News, and GitHub of content like issues submitted, closed issues, 
        number of stars on a repository, and overall StackOverflow activity.
        Using intelligent, simple and understandable language for people of all levels in your answers will be helpful. 
        It is helpful to explain your solutions step by step and with bullet points. 
        Try to avoid too many technical details, but use them when necessary. 
        I want you to reply with the solution, not write any explanations.
        """;

    public static string CreateQuestionPrompt(Post question)
    {
        if (string.IsNullOrEmpty(question.Body))
            throw new ArgumentNullException(nameof(question.Body));
        
        var content = $$"""
                      Title: {{question.Title}}
                      
                      Tags: {{string.Join(", ", question.Tags)}}
                      
                      {{question.Body}}
                      """;
        return content;
    }

    protected override async Task RunAsync(CreateAnswerTasks request, CancellationToken token)
    {
        var question = request.Post;
        if (question == null)
            throw new ArgumentNullException(nameof(request.Post));
        
        var log = Request.CreateJobLogger(jobs, logger);
        if (request.ModelUsers == null || request.ModelUsers.Count == 0)
        {
            log.LogError("Missing ModelUsers for question {Id}", question.Id);
            throw new ArgumentNullException(nameof(request.ModelUsers));
        }
        
        if (question.Body == null)
        {
            log.LogError("Missing Post Body for question {Id}", question.Id);
            throw new ArgumentNullException(nameof(request.Post));
        }

        var client = appConfig.CreateAiServerClient();

        foreach (var userName in request.ModelUsers)
        {
            ApplicationUser? modelUser;
            var startedAt = DateTime.UtcNow;
            try
            {
                modelUser = appConfig.GetModelUser(userName);
                if (modelUser?.Model == null)
                {
                    log.LogError("Model {UserName} not found", userName);
                    continue;
                }
            
                var prompt = CreateQuestionPrompt(question);
                var openAiChat = new OpenAiChat
                {
                    Model = modelUser.Model!,
                    Messages = [
                        new() { Role = "system", Content = SystemPrompt },
                        new() { Role = "user",   Content = prompt },
                    ],
                    Temperature = 0.7,
                    MaxTokens = 2048,
                };
                var replyTo = appConfig.BaseUrl.CombineWith("api", nameof(CreateAnswerCallback).AddQueryParams(new() {
                    [nameof(CreateAnswerCallback.PostId)] = question.Id,
                    [nameof(CreateAnswerCallback.UserId)] = modelUser.Id,
                }));
                
                log.LogInformation("Sending CreateOpenAiChat for Question {Id} Answer for {UserName}, replyTo: {ReplyTo}", 
                    question.Id, userName, replyTo);
                var response = await client.PostAsync(new CreateOpenAiChat
                {
                    Tag = "pvq",
                    ReplyTo = replyTo,
                    Request = openAiChat
                }, token);

                log.LogInformation("Completed CreateOpenAiChat for Question {Id} Answer for {UserName} in {Ms}: OK", 
                    question.Id, userName, (int)(DateTime.UtcNow - startedAt).TotalMilliseconds);
            }
            catch (Exception e)
            {
                log.LogError(e, "Completed CreateOpenAiChat for Question {Id} Answer for {UserName} in {Ms}: {Status}", 
                    question.Id, userName, (int)(DateTime.UtcNow - startedAt).TotalMilliseconds, 
                    $"{(e as WebServiceException)?.ErrorCode ?? e.GetType().Name}: {e.Message}");
            }
        }
    }
}
