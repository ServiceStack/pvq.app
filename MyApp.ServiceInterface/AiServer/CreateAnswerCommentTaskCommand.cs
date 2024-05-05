using AiServer.ServiceModel;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;

namespace MyApp.ServiceInterface.AiServer;

public class CreateAnswerCommentTaskCommand(AppConfig appConfig) : IAsyncCommand<CreateAnswerCommentTask>
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

    public async Task ExecuteAsync(CreateAnswerCommentTask request)
    {
        var question = request.Question;

        request.AiRef ??= Guid.NewGuid().ToString("N");
        
        var answerPrompt = 
            $$"""
            ## Original Answer Attempt

            {{request.Answer.Body}}
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
            MaxTokens = 100,
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

        var client = appConfig.CreateAiServerClient();
        
        var api = await client.ApiAsync(new CreateOpenAiChat {
            RefId = request.AiRef,
            Tag = "pvq",
            Provider = null,
            ReplyTo = appConfig.BaseUrl.CombineWith("api", nameof(AnswerCommentCallback).AddQueryParams(new() {
                [nameof(AnswerCommentCallback.AnswerId)] = request.Answer.RefId,
                [nameof(AnswerCommentCallback.UserId)] = request.UserId,
                [nameof(AnswerCommentCallback.AiRef)] = request.AiRef,
            })),
            Request = openAiChat
        });

        api.ThrowIfError();
    }
}
