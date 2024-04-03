using System.Net;
using MyApp.Data;
using ServiceStack;
using MyApp.ServiceModel;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace MyApp.ServiceInterface;

public class QuestionServices(AppConfig appConfig, 
    QuestionsProvider questions, 
    RendererCache rendererCache, 
    WorkerAnswerNotifier answerNotifier) : Service
{
    private List<string> ValidateQuestionTags(List<string>? tags)
    {
        var validTags = (tags ?? []).Select(x => x.Trim().ToLower()).Where(x => appConfig.AllTags.Contains(x)).ToList();

        if (validTags.Count == 0)
            throw new ArgumentException("At least 1 tag is required", nameof(tags));

        if (validTags.Count > 5)
            throw new ArgumentException("Maximum of 5 tags allowed", nameof(tags));
        return validTags;
    }
    
    public async Task<object> Get(GetAllAnswers request)
    {
        var question = await questions.GetQuestionAsync(request.Id);
        var modelNames = question.Question?.Answers.Where(x => !string.IsNullOrEmpty(x.Model)).Select(x => x.Model).ToList();
        var humanAnswers = question.Question?.Answers.Where(x => string.IsNullOrEmpty(x.Model)).Select(x => x.Id.SplitOnFirst("-")[1]).ToList();
        modelNames?.AddRange(humanAnswers ?? []);
        var answers = question
            .GetAnswerFiles()
            .ToList();

        return new GetAllAnswersResponse
        {
            Answers = modelNames ?? new List<string>()
        };
    }
    

    public async Task<object> Any(AskQuestion request)
    {
        var tags = ValidateQuestionTags(request.Tags);

        var userName = GetUserName();
        var now = DateTime.UtcNow;
        var postId = (int)appConfig.GetNextPostId();
        var title = request.Title.Trim();
        var body = request.Body.Trim();
        var slug = request.Title.GenerateSlug(200);
        var summary = request.Body.StripHtml().SubstringWithEllipsis(0, 200);

        var existingPost = await Db.SingleAsync(Db.From<Post>().Where(x => x.Title == title));
        if (existingPost != null)
            throw new ArgumentException($"Question with title '{title}' already used in question {existingPost.Id}", nameof(Post.Title));
        
        Post createPost() => new()
        {
            Id = postId,
            PostTypeId = 1,
            Title = title,
            Tags = tags,
            Slug = slug,
            Summary = summary,
            CreationDate = now,
            CreatedBy = userName,
            LastActivityDate = now,
            Body = body,
            RefId = request.RefId,
        };

        var post = createPost();
        var dbPost = createPost();
        dbPost.Body = null;
        MessageProducer.Publish(new DbWrites
        {
            CreatePost = dbPost,
            CreatePostJobs = appConfig.GetAnswerModelsFor(userName)
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
            RedirectTo = $"/answers/{post.Id}/{post.Slug}"
        };
    }

    public async Task<object> Any(DeleteQuestion request)
    {
        await questions.DeleteQuestionFilesAsync(request.Id);
        rendererCache.DeleteCachedQuestionPostHtml(request.Id);
        MessageProducer.Publish(new DbWrites
        {
            DeletePost = request.Id,
        });
        MessageProducer.Publish(new SearchTasks
        {
            DeletePost = request.Id,
        });

        if (request.ReturnUrl != null && request.ReturnUrl.StartsWith('/') && request.ReturnUrl.IndexOf(':') < 0)
            return HttpResult.Redirect(request.ReturnUrl, HttpStatusCode.TemporaryRedirect);
        
        return new EmptyResponse();
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
        
        // Rewind last Id if it was latest question
        var maxPostId = Db.Scalar<int>("SELECT MAX(Id) FROM Post");
        AppConfig.Instance.SetInitialPostId(Math.Max(100_000_000, maxPostId));
        
        answerNotifier.NotifyNewAnswer(request.PostId, post.CreatedBy);

        return new AnswerQuestionResponse();
    }

    /* /100/000
     *   001.json <Post>
     * Edit 1:
     *   001.json <Post> Updated
     *   edit.q.100000001-user_20240301-1200.json // original question
     */
    public async Task<object> Any(UpdateQuestion request)
    {
        var question = await Db.SingleByIdAsync<Post>(request.Id);
        if (question == null)
            throw HttpError.NotFound("Question does not exist");
        
        var userName = GetUserName();
        var isModerator = Request.GetClaimsPrincipal().HasRole(Roles.Moderator);
        if (!isModerator && question.CreatedBy != userName)
        {
            var userInfo = await Db.SingleAsync<UserInfo>(x => x.UserName == userName);
            if (userInfo.Reputation < 10)
                throw HttpError.Forbidden("You need at least 10 reputation to Edit other User's Questions.");
        }

        question.Title = request.Title;
        question.Tags = ValidateQuestionTags(request.Tags);
        question.Slug = request.Title.GenerateSlug(200);
        question.Summary = request.Body.StripHtml().SubstringWithEllipsis(0, 200);
        question.Body = request.Body;
        question.ModifiedBy = userName;
        question.LastActivityDate = DateTime.UtcNow;
        question.LastEditDate = question.LastActivityDate;

        MessageProducer.Publish(new DbWrites
        {
            UpdatePost = question,
        });
        await questions.SaveQuestionEditAsync(question);

        return new UpdateQuestionResponse
        {
            Result = question
        };
    }

    /* /100/000
     *   001.a.model.json <OpenAI>
     * Edit 1:
     *   001.h.model.json <Post>
     *   edit.a.100000001-model_20240301-1200.json // original model answer, Modified Date <OpenAI>
     */
    public async Task<object> Any(UpdateAnswer request)
    {
        var answerFile = await questions.GetAnswerFileAsync(request.Id);
        if (answerFile == null)
            throw HttpError.NotFound("Answer does not exist");

        var userName = GetUserName();
        var isModerator = Request.GetClaimsPrincipal().HasRole(Roles.Moderator);
        if (!isModerator && !answerFile.Name.Contains(userName))
        {
            var userInfo = await Db.SingleAsync<UserInfo>(x => x.UserName == userName);
            if (userInfo.Reputation < 100)
                throw HttpError.Forbidden("You need at least 100 reputation to Edit other User's Answers.");
        }

        await questions.SaveAnswerEditAsync(answerFile, userName, request.Body, request.EditReason);

        return new UpdateAnswerResponse();
    }

    public async Task<object> Any(GetQuestion request)
    {
        var question = await Db.SingleByIdAsync<Post>(request.Id);
        if (question == null)
            throw HttpError.NotFound($"Question {request.Id} not found");

        return new GetQuestionResponse
        {
            Result = question
        };
    }

    public async Task<object> Any(GetQuestionFile request)
    {
        var questionFile = await questions.GetQuestionFileAsync(request.Id);
        if (questionFile == null)
            throw HttpError.NotFound($"Question {request.Id} not found");
        
        //TODO: Remove Hack when all files are converted to camelCase
        var json = await questionFile.ReadAllTextAsync();
        if (json.Trim().StartsWith("{\"Id\":"))
        {
            var post = json.FromJson<Post>();
            var camelCaseJson = questions.ToJson(post);
            return new HttpResult(camelCaseJson, MimeTypes.Json);
        }
        
        return new HttpResult(json, MimeTypes.Json);
    }
    
    public async Task<object> Get(GetAnswerFile request)
    {
        var answerFile = await questions.GetAnswerFileAsync(request.Id);
        if (answerFile == null)
            throw HttpError.NotFound("Answer does not exist");

        var json = await answerFile.ReadAllTextAsync();
        return new HttpResult(json, MimeTypes.Json);
    }

    public async Task<object> Any(GetAnswerBody request)
    {
        var answerFile = await questions.GetAnswerFileAsync(request.Id);
        if (answerFile == null)
            throw HttpError.NotFound("Answer does not exist");

        var json = await answerFile.ReadAllTextAsync();
        if (answerFile.Name.Contains(".a."))
        {
            var obj = (Dictionary<string,object>)JSON.parse(json);
            var choices = (List<object>) obj["choices"];
            var choice = (Dictionary<string,object>)choices[0];
            var message = (Dictionary<string,object>)choice["message"];
            var body = (string)message["content"];
            return new HttpResult(body, MimeTypes.PlainText);
        }
        else
        {
            var answer = json.FromJson<Post>();
            return new HttpResult(answer.Body, MimeTypes.PlainText);
        }
    }

    /// <summary>
    /// DEBUG
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<object> Any(CreateRankingPostJob request)
    {
        MessageProducer.Publish(new DbWrites
        {
            CreatePostJobs = new List<PostJob>
            {
                new PostJob
                {
                    PostId = request.PostId,
                    Model = "rank",
                    Title = $"rank-{request.PostId}",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = nameof(DbWrites),
                }
            }
        });
        return "{ \"success\": true }";
    }

    public async Task<object> Any(CreateWorkerAnswer request)
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
        
        rendererCache.DeleteCachedQuestionPostHtml(request.PostId);

        if (request.PostJobId != null)
        {
            MessageProducer.Publish(new DbWrites {
                AnswerAddedToPost = request.PostId,
                CompleteJobIds = [request.PostJobId.Value]
            });
        }
        
        await questions.SaveModelAnswerAsync(request.PostId, request.Model, json);
        
        answerNotifier.NotifyNewAnswer(request.PostId, request.Model);
        
        return new IdResponse { Id = $"{request.PostId}" };
    }

    public async Task<object> Any(RankAnswers request)
    {
        return new IdResponse { Id = $"{request.PostId}" };
    }

    public async Task<object> Any(CreateComment request)
    {
        var question = await AssertValidQuestion(request.Id);
        if (question.LockedDate != null)
            throw HttpError.Conflict($"{question.GetPostType()} is locked");

        var postId = question.Id;
        var meta = await questions.GetMetaAsync(postId);
        
        meta.Comments ??= new();
        var comments = meta.Comments.GetOrAdd(request.Id, key => new());
        var body = request.Body.Replace("\r\n", " ").Replace('\n', ' ');
        comments.Add(new Comment
        {
            Body = body,
            Created = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            CreatedBy = GetUserName(),
        });

        await questions.SaveMetaAsync(postId, meta);
        
        return new CommentsResponse
        {
            Comments = comments
        };
    }

    private async Task<Post> AssertValidQuestion(string refId)
    {
        var postId = refId.LeftPart('-').ToInt();
        var postType = refId.IndexOf('-') >= 0 ? "Answer" : "Question";

        var question = await Db.SingleByIdAsync<Post>(postId);
        if (question == null)
            throw HttpError.NotFound($"{postType} {postId} not found");

        return question;
    }

    public async Task<object> Any(DeleteComment request)
    {
        var question = await AssertValidQuestion(request.Id);

        var userName = GetUserName();
        var isModerator = Request.GetClaimsPrincipal().HasRole(Roles.Moderator);
        if (userName != request.CreatedBy && !isModerator)
            throw HttpError.Forbidden("Only Moderators can delete other user's comments");
        
        var postId = question.Id;
        var meta = await questions.GetMetaAsync(postId);
        
        meta.Comments ??= new();
        if (meta.Comments.TryGetValue(request.Id, out var comments))
        {
            comments.RemoveAll(x => x.Created == request.Created && x.CreatedBy == request.CreatedBy);
            await questions.SaveMetaAsync(postId, meta);
            return new CommentsResponse { Comments = comments };
        }

        return new CommentsResponse { Comments = [] };
    }

    public async Task<object> Any(GetMeta request)
    {
        var postId = request.Id.LeftPart('-').ToInt();
        var metaFile = await questions.GetMetaFileAsync(postId);
        var metaJson = metaFile != null
            ? await metaFile.ReadAllTextAsync()
            : "{}";
        var meta = metaJson.FromJson<Meta>();
        return meta;
    }

    public object Any(GetUserReputations request)
    {
        var to = new GetUserReputationsResponse();
        foreach (var userName in request.UserNames.Safe())
        {
            to.Results[userName] = appConfig.GetReputation(userName);
        }
        return to;
    }

    private string GetUserName()
    {
        var userName = Request.GetClaimsPrincipal().GetUserName()
                       ?? throw new ArgumentNullException(nameof(ApplicationUser.UserName));
        return userName;
    }
}


/// <summary>
/// DEBUG
/// </summary>
[ValidateIsAuthenticated]
public class CreateRankingPostJob
{
    public int PostId { get; set; }
}

[ValidateIsAuthenticated]
public class GetAnswerFile
{
    /// <summary>
    /// Format is {PostId}-{UserName}
    /// </summary>
    public string Id { get; set; }
}

public class GetAllAnswersResponse
{
    public string Question { get; set; }
    public List<string> Answers { get; set; }
}

[ValidateIsAuthenticated]
public class GetAllAnswers : IReturn<GetAllAnswersResponse>, IGet
{
    public int Id { get; set; }
}
