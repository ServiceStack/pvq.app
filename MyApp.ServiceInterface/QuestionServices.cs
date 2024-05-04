using System.Net;
using System.Text.RegularExpressions;
using MyApp.Data;
using MyApp.ServiceInterface.App;
using ServiceStack;
using MyApp.ServiceModel;
using ServiceStack.IO;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class QuestionServices(AppConfig appConfig, 
    QuestionsProvider questions, 
    RendererCache rendererCache, 
    WorkerAnswerNotifier answerNotifier,
    ICommandExecutor executor) : Service
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

    public async Task<object> Get(GetAllAnswerModels request)
    {
        var question = await questions.GetQuestionAsync(request.Id);
        var modelNames = question.Question?.Answers
            .Where(x => x.CreatedBy != null && !appConfig.IsHuman(x.CreatedBy))
            .Select(x => x.CreatedBy!)
            .ToList() ?? [];

        return new GetAllAnswerModelsResponse
        {
            Results = modelNames
        };
    }
    
    static Regex AlphaNumericRegex = new("[^a-zA-Z0-9]", RegexOptions.Compiled);
    static Regex SingleWhiteSpaceRegex = new( @"\s+", RegexOptions.Multiline | RegexOptions.Compiled);

    public async Task<object> Any(FindSimilarQuestions request)
    {
        var searchPhrase = AlphaNumericRegex.Replace(request.Text, " ");
        searchPhrase = SingleWhiteSpaceRegex.Replace(searchPhrase, " ").Trim();
        if (searchPhrase.Length < 15)
            throw new ArgumentException("Search text must be at least 20 characters", nameof(request.Text));

        using var dbSearch = HostContext.AppHost.GetDbConnection(Databases.Search);
        var q = dbSearch.From<PostFts>()
            .Where("Body match {0} AND instr(RefId,'-') == 0", searchPhrase)
            .OrderBy("rank")
            .Limit(10);
        
        var results = await dbSearch.SelectAsync(q);
        var posts = await Db.PopulatePostsAsync(results);

        return new FindSimilarQuestionsResponse
        {
            Results = posts
        };
    }

    public async Task<object> Any(AskQuestion request)
    {
        var tags = ValidateQuestionTags(request.Tags);

        var userName = GetUserName();
        var now = DateTime.UtcNow;
        var title = request.Title.Trim();
        var body = request.Body.Trim();
        var slug = request.Title.GenerateSlug(200);
        var summary = request.Body.GenerateSummary();

        var existingPost = await Db.SingleAsync(Db.From<Post>().Where(x => x.Title == title));
        if (existingPost != null)
            throw new ArgumentException($"Question with title '{title}' already used in question {existingPost.Id}", nameof(Post.Title));

        var refUrn = request.RefUrn;
        var postId = refUrn != null && refUrn.StartsWith("stackoverflow.com:")
               && int.TryParse(refUrn.LastRightPart(':'), out var stackoverflowPostId)
               && !await Db.ExistsAsync<Post>(x => x.Id == stackoverflowPostId)
            ? stackoverflowPostId
            :  (int)appConfig.GetNextPostId();
            
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
            RefId = $"{postId}",
            RefUrn = refUrn,
        };

        var post = createPost();
        var dbPost = createPost();
        MessageProducer.Publish(new DbWrites
        {
            CreatePost = dbPost,
        });
        
        MessageProducer.Publish(new AiServerTasks
        {
            CreateAnswerTasks = new() {
                Post = post,
                ModelUsers = appConfig.GetAnswerModelUsersFor(userName),
            }
        });

        MessageProducer.Publish(new DiskTasks
        {
            SaveQuestion = post,
        });
       
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
            DeletePost = new() { Ids = [request.Id] },
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
        var answer = new Post
        {
            ParentId = request.PostId,
            Summary = request.Body.GenerateSummary(),
            CreationDate = now,
            CreatedBy = userName,
            LastActivityDate = now,
            Body = request.Body,
            RefUrn = request.RefUrn,
            RefId = $"{request.PostId}-{userName}"
        };
        
        MessageProducer.Publish(new DbWrites {
            CreateAnswer = answer,
            AnswerAddedToPost = new() { Id = request.PostId},
        });
        
        rendererCache.DeleteCachedQuestionPostHtml(answer.Id);

        await questions.SaveHumanAnswerAsync(answer);

        MessageProducer.Publish(new DbWrites
        {
            SaveStartingUpVotes = new()
            {
                Id = answer.RefId!,
                PostId = request.PostId,
                StartingUpVotes = 0,
                CreatedBy = userName,
            }
        });
        
        answerNotifier.NotifyNewAnswer(request.PostId, answer.CreatedBy);

        var userId = Request.GetClaimsPrincipal().GetUserId();
        MessageProducer.Publish(new AiServerTasks
        {
            CreateRankAnswerTask = new CreateRankAnswerTask {
                AnswerId = answer.RefId!,
                UserId = userId!,
            } 
        });
        
        MessageProducer.Publish(new SearchTasks {
            AddAnswerToIndex = answer.RefId
        });

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
        question.Summary = request.Body.GenerateSummary();
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

    private async Task<IVirtualFile?> AssetQuestionFile(int id)
    {
        var questionFile = await questions.GetQuestionFileAsync(id);
        if (questionFile == null)
            throw HttpError.NotFound($"Question {id} not found");
        return questionFile;
    }

    public async Task<object> Any(GetQuestionFile request)
    {
        var questionFile = await AssetQuestionFile(request.Id);
        var json = await questionFile.ReadAllTextAsync();
        return new HttpResult(json, MimeTypes.Json);
    }

    public async Task<object> Any(GetQuestionBody request)
    {
        var questionFile = await AssetQuestionFile(request.Id);
        var json = await questionFile.ReadAllTextAsync();
        var post = json.FromJson<Post>();
        return post.Body;
    }
    
    public async Task<object> Get(GetAnswerFile request)
    {
        var answerFile = await questions.GetAnswerFileAsync(request.Id);
        if (answerFile == null)
            throw HttpError.NotFound("Answer does not exist");

        var json = await answerFile.ReadAllTextAsync();
        return new HttpResult(json, MimeTypes.Json);
    }

    public async Task<object> Get(GetAnswer request)
    {
        var answerFile = await questions.GetAnswerFileAsync(request.Id);
        if (answerFile == null)
            throw HttpError.NotFound("Answer does not exist");

        var post = await questions.GetAnswerAsPostAsync(answerFile);
        return new GetAnswerResponse
        {
            Result = post
        };
    }

    public async Task<object> Any(GetAnswerBody request)
    {
        var body = await questions.GetAnswerBodyAsync(request.Id);
        if (body == null)
            throw HttpError.NotFound("Answer does not exist");

        return new HttpResult(body, MimeTypes.PlainText);
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
            CreatePostJobs = new()
            {
                PostJobs = [
                    new PostJob
                    {
                        PostId = request.PostId,
                        Model = "rank",
                        Title = $"rank-{request.PostId}",
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = nameof(DbWrites),
                    }
                ]
            }
        });
        return new EmptyResponse();
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

        json = json.Trim();
        if (string.IsNullOrEmpty(json))
            throw new ArgumentException("Json is required", nameof(request.Json));
        if (!json.StartsWith('{'))
            throw new ArgumentException("Invalid Json", nameof(request.Json));
        
        rendererCache.DeleteCachedQuestionPostHtml(request.PostId);

        if (request.PostJobId != null)
        {
            MessageProducer.Publish(new DbWrites {
                AnswerAddedToPost = new() { Id = request.PostId },
                CompletePostJobs = new() { Ids = [request.PostJobId.Value] }
            });
        }
        
        await questions.SaveModelAnswerAsync(request.PostId, request.Model, json);
        
        answerNotifier.NotifyNewAnswer(request.PostId, request.Model);

        // Only add notifications for answers older than 1hr
        var post = await Db.SingleByIdAsync<Post>(request.PostId);
        if (post?.CreatedBy != null && DateTime.UtcNow - post.CreationDate > TimeSpan.FromHours(1))
        {
            var userName = appConfig.GetUserName(request.Model);
            var body = questions.GetModelAnswerBody(json);
            var cleanBody = body.StripHtml()?.Trim();
            if (!string.IsNullOrEmpty(cleanBody))
            {
                MessageProducer.Publish(new DbWrites {
                    CreateNotification = new()
                    {
                        UserName = post.CreatedBy,
                        PostId = post.Id,
                        Type = NotificationType.NewAnswer,
                        CreatedDate = DateTime.UtcNow,
                        RefId = $"{post.Id}-{userName}",
                        Summary = cleanBody.GenerateNotificationSummary(),
                        RefUserName = userName,
                    },
                });
            }
        }
        
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
        var newComment = new Comment
        {
            Body = body,
            Created = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            CreatedBy = GetUserName(),
        };
        comments.Add(newComment);
        
        MessageProducer.Publish(new DbWrites
        {
            NewComment = new()
            {
                RefId = request.Id,
                Comment = newComment,
            },
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
        
        MessageProducer.Publish(new DbWrites
        {
            DeleteComment = request, 
        });
        
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

    public async Task<object> Any(ImportQuestion request)
    {
        var command = executor.Command<ImportQuestionCommand>();
        await executor.ExecuteAsync(command, request);
        return new ImportQuestionResponse
        {
            Result = command.Result
                ?? throw new Exception("Import failed")
        };
    }
}


/// <summary>
/// DEBUG
/// </summary>
[ValidateIsAuthenticated]
public class CreateRankingPostJob : IReturn<EmptyResponse>
{
    public int PostId { get; set; }
}

[ValidateHasRole(Roles.Moderator)]
public class GetAnswerFile : IGet, IReturn<string>
{
    /// <summary>
    /// Format is {PostId}-{UserName}
    /// </summary>
    public string Id { get; set; }
}
public class GetAllAnswerModelsResponse
{
    public List<string> Results { get; set; }
}

public class GetAnswer : IGet, IReturn<GetAnswerResponse>
{
    /// <summary>
    /// Format is {PostId}-{UserName}
    /// </summary>
    public string Id { get; set; }
}
public class GetAnswerResponse
{
    public Post Result { get; set; }
}

[ValidateIsAuthenticated]
public class GetAllAnswerModels : IReturn<GetAllAnswerModelsResponse>, IGet
{
    public int Id { get; set; }
}
