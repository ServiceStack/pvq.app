using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Jobs;

namespace MyApp.ServiceInterface.AiServer;

public class CreateRankAnswerTask
{
    public string AnswerId { get; set; }
    public string UserId { get; set; }
}

[Tag(Tags.AI)]
public class CreateRankAnswerTaskCommand(ILogger<CreateRankAnswerTaskCommand> logger, IBackgroundJobs jobs, 
    AppConfig appConfig, QuestionsProvider questions) 
    : AsyncCommand<CreateRankAnswerTask>
{
    //https://github.com/f/awesome-chatgpt-prompts?tab=readme-ov-file#act-as-a-tech-reviewer
    public const string SystemPrompt = 
        """
        I want you to act as a tech reviewer that votes on the quality and relevance of answers to a given question. 
        I will give you the user's question and the answer that you should review and respond with a score out of 10.
        Before giving a score, give a critique of the answer based on quality and relevance to the user's question. 
        """;

    protected override async Task RunAsync(CreateRankAnswerTask request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        var postId = request.AnswerId.LeftPart('-').ToInt();
        var question = await questions.GetLocalQuestionFiles(postId).GetQuestionAsync();
        if (question == null)
            throw HttpError.NotFound("Question not found");
        var answerBody = await questions.GetAnswerBodyAsync(request.AnswerId);

        var content = $$"""
                      Below I have a user question and an answer to the user question. I want you to give a score out of 10 based on the quality in relation to the original user question. 
                      
                      ## Original User Question
                      
                      Title: {{question.Post.Title}}
                      Body:
                      {{question.Post.Body}}
                      Tags: {{string.Join(", ", question.Post.Tags)}}
                      ---
                      
                      Critique the below answer to justify your score, providing a brief explanation before returning the simple JSON object showing your reasoning and score.
                      
                      Think about the answer given in relation to the original user question. Use the tags to help you understand the context of the question.
                      
                      ## Answer Attempt
                      
                      {{answerBody}}
                      ---
                      
                      Now review and score the answer above out of 10.
                      
                      Concisely articulate what a good answer needs to contain and how the answer provided does or does not meet those criteria.
                      
                      - If the answer has mistakes or does not address all the question details, score it between 0-2. 
                      - If the answer is correct, but could be improved, score it between 3-6. 
                      - If the answer is correct and provides a good explanation, score it between 7-9.
                      - If the answer is perfect and provides a clear and concise explanation, score it 10. 
                      
                      Because these are coding questions, mistakes in the code are critical and should be scored lower. Look closely at the syntax and logic of the code for any mistakes. Missing mistakes in reviews leads to a failed review, and many answers are not correct.
                      
                      You MUST provide a JSON object with the following schema:
                      
                      ## Example JSON Response
                      
                      ```json
                      {
                          "reason": "Your reason goes here. Below score is only an example. Score should reflect the review of the answer.",
                          "score": 1
                      }
                      ```
                      
                      Use code fences, aka triple backticks, to encapsulate your JSON object.
                      """;

        var replyTo = appConfig.BaseUrl.CombineWith("api", nameof(RankAnswerCallback).AddQueryParams(new() {
            [nameof(RankAnswerCallback.PostId)] = postId,
            [nameof(RankAnswerCallback.UserId)] = request.UserId,
            [nameof(RankAnswerCallback.Grader)] = "mixtral",
        }));
        
        var startedAt = DateTime.UtcNow;
        log.LogInformation("Sending CreateOpenAiChat for Question {Id} Rank Answer, replyTo: {ReplyTo}", 
            question.Id, replyTo);
        
        var client = appConfig.CreateAiServerClient();
        var api = await client.ApiAsync(new CreateOpenAiChat {
            RefId = Guid.NewGuid().ToString("N"),
            Tag = "pvq",
            Provider = null,
            ReplyTo = replyTo,
            Request = new()
            {
                Model = "mixtral",
                Messages = [
                    new() { Role = "system", Content = SystemPrompt },
                    new() { Role = "user", Content = content },
                ],
                Temperature = 0.1,
                MaxTokens = 1024,
                Stream = false,
            }
        });

        log.LogInformation("Completed CreateOpenAiChat for Question {Id} Rank Answer in {Ms}: {Status}", 
            question.Id, (int)(DateTime.UtcNow - startedAt).TotalMilliseconds, 
            api.Error == null ? "OK" : $"{api.Error.ErrorCode}: {api.Error.Message}");
        api.ThrowIfError();
    }
}
