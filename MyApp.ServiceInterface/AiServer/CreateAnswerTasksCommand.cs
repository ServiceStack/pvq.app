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
        var client = appConfig.CreateAiServerClient();

        var question = request.Post;
        if (question?.Body == null)
            throw new ArgumentNullException(nameof(request.Post));

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
            
                var response = await client.PostAsync(new CreateOpenAiChat
                {
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
                log.LogError(e, "Failed to CreateOpenAiChat Model for {UserName}: {Message}", userName, e.Message);
            }
        }
    }
}
