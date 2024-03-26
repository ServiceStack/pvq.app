using MyApp.Data;
using ServiceStack;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface;

public class QuestionServices(AppConfig appConfig, QuestionsProvider questions, RendererCache rendererCache) : Service
{
    public async Task<object> Any(AskQuestion request)
    {
        var tags = (request.Tags ?? []).Select(x => x.Trim().ToLower()).Where(x => appConfig.AllTags.Contains(x)).ToList();

        if (tags.Count == 0)
            throw new ArgumentException("At least 1 tag is required", nameof(request.Tags));

        if (tags.Count > 5)
            throw new ArgumentException("Maximum of 5 tags allowed", nameof(request.Tags));

        var userName = GetUserName();
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
            CreatedBy = userName,
            LastActivityDate = now,
            Body = request.Body,
            RefId = request.RefId,
        };

        MessageProducer.Publish(new DbWrites
        {
            CreatePost = post,
            CreatePostJobs = questions.GetAnswerModelsFor(userName)
                .Select(model => new PostJob
                {
                    PostId = post.Id,
                    Model = model,
                    Title = request.Title,
                    CreatedBy = userName,
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

    public async Task<object> Any(AnswerQuestion request)
    {
        var userName = GetUserName();
        var now = DateTime.UtcNow;
        var post = new Post
        {
            ParentId = request.PostId,
            Summary = request.Body.StripHtml().SubstringWithEllipsis(0,200),
            CreationDate = now,
            CreatedBy = userName,
            LastActivityDate = now,
            Body = request.Body,
            RefId = request.RefId,
        };
        
        await questions.SaveHumanAnswerAsync(post);
        rendererCache.DeleteCachedQuestionPostHtml(post.Id);
        return new AnswerQuestionResponse();
    }

    public async Task<object> Any(GetQuestionFile request)
    {
        var questionFiles = await questions.GetQuestionFilesAsync(request.Id);
        var file = questionFiles.GetQuestionFile();
        if (file == null)
            throw HttpError.NotFound($"Question {request.Id} not found");
        return new HttpResult(file, MimeTypes.Json);
    }

    public async Task Any(CreateWorkerAnswer request)
    {
        var json = request.Json;
        if (string.IsNullOrEmpty(json))
        {
            var file = base.Request!.Files.FirstOrDefault();
            if (file != null)
            {
                using var reader = new StreamReader(file.InputStream);
                json = await reader.ReadToEndAsync();
            }
        }

        json = json?.Trim();
        if (string.IsNullOrEmpty(json))
            throw new ArgumentException("Json is required", nameof(request.Json));
        if (!json.StartsWith('{'))
            throw new ArgumentException("Invalid Json", nameof(request.Json));
        
        if (request.PostJobId != null)
        {
            MessageProducer.Publish(new DbWrites {
                AnswerAddedToPost = request.PostId,
                CompleteJobIds = [request.PostJobId.Value]
            });
        }
        
        await questions.SaveModelAnswerAsync(request.PostId, request.Model, json);
    }

    private string GetUserName()
    {
        var userName = Request.GetClaimsPrincipal().GetUserName()
                       ?? throw new ArgumentNullException(nameof(ApplicationUser.UserName));
        return userName;
    }
}
