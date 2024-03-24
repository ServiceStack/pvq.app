using MyApp.Data;
using ServiceStack;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface;

public class QuestionServices(AppConfig appConfig, QuestionsProvider questions) : Service
{
    public async Task<object> Any(AskQuestion request)
    {
        var tags = (request.Tags ?? []).Select(x => x.Trim().ToLower()).Where(x => appConfig.AllTags.Contains(x)).ToList();

        if (tags.Count == 0)
            throw new ArgumentException("At least 1 tag is required", nameof(request.Tags));

        if (tags.Count > 5)
            throw new ArgumentException("Maximum of 5 tags allowed", nameof(request.Tags));

        var userName = Request.GetClaimsPrincipal().GetUserName();

        var now = DateTime.UtcNow;
        var post = new Post
        {
            Id = (int)appConfig.GetNextPostId(),
            PostTypeId = 1,
            Title = request.Title,
            Tags = tags,
            Slug = request.Title.GenerateSlug(200),
            Summary = request.Body.StripHtml().SubstringWithEllipsis(0,200),
            CreationDate = now,
            LastActivityDate = now,
            CreatedBy = userName,
            Body = request.Body,
        };

        MessageProducer.Publish(new DbWrites
        {
            CreatePost = post,
            CreatePostJobs = questions.GetAnswerModelsFor(userName)
                .Select(model => new PostJob
                {
                    PostId = post.Id,
                    Title = request.Title,
                    Body = request.Body,
                    Tags = tags,
                    Model = model,
                    CreatedDate = now,
                }).ToList(),
        });

        await questions.SaveQuestionAsync(post);
       
        return new AskQuestionResponse
        {
            Id = post.Id,
            Slug = post.Slug,
            RedirectTo = $"/questions/{post.Id}/{post.Slug}"
        };
    }

    public async Task Any(CreateWorkerAnswer request)
    {
        if (request.PostJobId != null)
        {
            MessageProducer.Publish(new DbWrites {
                AnswerAddedToPost = request.PostId,
                CompleteJobIds = [request.PostJobId.Value]
            });
        }
        
        await questions.SaveAnswerAsync(request.PostId, request.Model, request.Json);
    }
}
