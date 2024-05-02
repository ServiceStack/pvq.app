using AiServer.ServiceModel;
using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;

namespace MyApp.ServiceInterface.AiServer;

public class CreateAnswerTasksCommand(ILogger<CreateAnswerTasksCommand> log, 
    AppConfig appConfig, QuestionsProvider questions) : IAsyncCommand<CreateAnswerTasks>
{
    const string SystemPrompt = "You are a friendly AI Assistant that helps answer developer questions. Think step by step and assist the user with their question, ensuring that your answer is relevant, on topic and provides actionable advice with code examples as appropriate.";

    public async Task ExecuteAsync(CreateAnswerTasks request)
    {
        var question = request.Post;
        if (question == null)
            throw new ArgumentNullException(nameof(request.Post));
        
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
            try
            {
                modelUser = appConfig.GetModelUser(userName);
                if (modelUser?.Model == null)
                {
                    log.LogError("Model {UserName} not found", userName);
                    continue;
                }
            
                log.LogInformation("Creating Question {Id} OpenAiChat Model for {UserName} to AI Server", question.Id, userName);
                var response = await client.PostAsync(new CreateOpenAiChat
                {
                    Tag = "pvq",
                    ReplyTo = appConfig.BaseUrl.CombineWith("api", nameof(CreateAnswerCallback).AddQueryParams(new() {
                        [nameof(CreateAnswerCallback.PostId)] = question.Id,
                        [nameof(CreateAnswerCallback.UserId)] = modelUser.Id,
                    })),
                    Request = new OpenAiChat
                    {
                        Model = modelUser.Model!,
                        Messages = [
                            new() { Role = "system", Content = SystemPrompt },
                            new() { Role = "user",   Content = question.Body },
                        ],
                        Temperature = 0.7,
                        MaxTokens = 2048,
                    }
                });
            }
            catch (Exception e)
            {
                log.LogError(e, "Failed to Creating Question {Id} OpenAiChat Model for {UserName}", question.Id, userName);
            }
        }
    }
}
