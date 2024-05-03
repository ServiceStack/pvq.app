using Microsoft.AspNetCore.Identity;
using MyApp.Data;
using MyApp.ServiceInterface.Renderers;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class AdminServices(AppConfig appConfig, ICommandExecutor executor, UserManager<ApplicationUser> userManager,
    QuestionsProvider questions)
    : Service
{
    private static readonly List<string> initUserNames = new()
    {
        "admin", "pvq", "stackoverflow", "most-voted",
        "accepted", "phi", "gemma-2b", "qwen-4b",
        "codellama", "llama3-8b", "llama3-70b", "gemma",
        "deepseek-coder", "mistral", "mixtral", "gemini-pro",
        "deepseek-coder-33b", "gpt4-turbo", "gpt3.5-turbo",
        "claude3-haiku", "claude3-sonnet", "claude3-opus",
        "command-r", "command-r-plus", "wizardlm", "servicestack", 
        "reddit", "discourse", "twitter", "threads", "mastodon"
    };

    public async Task<object> Any(Sync request)
    {
        if (request.Tasks != null)
        {
            var db = Db;
            var tasks = request.Tasks;
            if (tasks.Contains(nameof(AppConfig.ResetInitialPostId)))
                appConfig.ResetInitialPostId(db);
            if (tasks.Contains(nameof(AppConfig.ResetUsersReputation)))
                appConfig.ResetUsersReputation(db);
            if (tasks.Contains(nameof(AppConfig.ResetUsersQuestions)))
                appConfig.ResetUsersQuestions(db);
            if (tasks.Contains(nameof(AppConfig.ResetUsersUnreadAchievements)))
                appConfig.ResetUsersUnreadAchievements(db);
            if (tasks.Contains(nameof(AppConfig.ResetUsersUnreadNotifications)))
                appConfig.ResetUsersUnreadNotifications(db);
            if (tasks.Contains(nameof(AppConfig.UpdateUsersReputation)))
                appConfig.UpdateUsersReputation(db);
            if (tasks.Contains(nameof(AppConfig.UpdateUsersQuestions)))
                appConfig.UpdateUsersQuestions(db);
        }
        return new StringResponse { Result = "OK" };
    }

    public async Task<object?> Any(GenerateMeta request)
    {
        var regenerateMeta = executor.Command<RegenerateMetaCommand>();
        await executor.ExecuteAsync(regenerateMeta, new RegenerateMeta
        {
            ForPost = request.Id
        });
        
        return regenerateMeta.Question;
    }

    public async Task<object> Any(AdminResetCommonPassword request)
    {
        // Get common password from environment variable
        var commonPassword = AppConfig.Instance.MasterPassword;
        if (string.IsNullOrEmpty(commonPassword))
            throw new ArgumentNullException("MasterPassword");

        var updatedUsers = new List<string>();
        
        // Reset all users password to common password
        foreach (var userName in initUserNames)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user == null) continue;
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var identityResult = await userManager.ResetPasswordAsync(user, token, commonPassword);
            if (identityResult.Succeeded)
            {
                updatedUsers.Add(userName);
            }
        }
        
        return new AdminResetCommonPasswordResponse { UpdatedUsers = updatedUsers };
    }
    
    public async Task<object?> Any(ResaveQuestionFromFile request)
    {
        var post = await questions.GetQuestionFileAsPostAsync(request.Id);
        if (post == null)
            throw HttpError.NotFound("Post not found");

        var refId = $"{request.Id}";
        await Db.SaveAsync(post);
        var statTotal = await Db.SingleAsync(Db.From<StatTotals>().Where(x => x.Id == refId));
        if (statTotal != null)
        {
            await Db.InsertAsync(new StatTotals
            {
                Id = refId,
                PostId = post.Id,
                ViewCount = 0,
                FavoriteCount = 0,
                UpVotes = 0,
                DownVotes = 0,
                StartingUpVotes = 0,
                CreatedBy = post.CreatedBy,
            });
        }
        appConfig.ResetInitialPostId(Db);
        return post;
    }

    public async Task<object> Any(RankAnswer request)
    {
        var answer = await questions.GetAnswerAsPostAsync(request.Id);
        if (answer == null)
            throw HttpError.NotFound("Answer not found");
        
        var answerCreator = !string.IsNullOrEmpty(answer.CreatedBy)
            ? await Db.ScalarAsync<string>(Db.From<ApplicationUser>().Where(x => x.UserName == answer.CreatedBy).Select(x => x.Id))
            : null;
        
        if (answerCreator == null)
            throw HttpError.NotFound($"Answer Creator '{answer.CreatedBy}' not found");
        
        MessageProducer.Publish(new AiServerTasks
        {
            CreateRankAnswerTask = new CreateRankAnswerTask {
                AnswerId = answer.RefId!,
                UserId = answerCreator,
            } 
        });

        return answer;
    }
}
