using Microsoft.AspNetCore.Identity;
using MyApp.Data;
using MyApp.ServiceInterface.App;
using ServiceStack;
using MyApp.ServiceModel;
using ServiceStack.IO;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;
using SixLabors.ImageSharp.Formats.Png;

namespace MyApp.ServiceInterface;

public class UserServices(
    IBackgroundJobs jobs,
    AppConfig appConfig, 
    R2VirtualFiles r2, 
    ImageCreator imageCreator, 
    UserManager<ApplicationUser> userManager) : Service
{
    private const string AppData = "/App_Data";
    
    public async Task<object> Any(UpdateUserProfile request)
    {
        var userName = Request.GetClaimsPrincipal().Identity!.Name!;
        var file = base.Request!.Files.FirstOrDefault();

        if (file != null)
        {
            var userProfileDir = $"/profiles/{userName[..2]}/{userName}"; 
            var origPath = userProfileDir.CombineWith(file.FileName);
            var fileName = $"{file.FileName.LastLeftPart('.')}_128.{file.FileName.LastRightPart('.')}";
            var profilePath = userProfileDir.CombineWith(fileName);
            var originalMs = await file.InputStream.CopyToNewMemoryStreamAsync();
            var resizedMs = await ImageUtils.CropAndResizeAsync(originalMs, 128, 128, PngFormat.Instance);
            
            await VirtualFiles.WriteFileAsync(AppData.CombineWith(origPath), originalMs);
            await VirtualFiles.WriteFileAsync(AppData.CombineWith(profilePath), resizedMs);

            Db.UpdateOnly(() => new ApplicationUser {
                ProfilePath = profilePath,
            }, x => x.UserName == userName);
            
            jobs.RunCommand<DiskTasksCommand>(new DiskTasks {
                SaveFile = new() {
                    FilePath = origPath,
                    Stream = originalMs,
                }
            });
            jobs.RunCommand<DiskTasksCommand>(new DiskTasks {
                SaveFile = new() {
                    FilePath = profilePath,
                    Stream = resizedMs,
                }
            });
        }
        
        return new UpdateUserProfileResponse();
    }

    public async Task<HttpResult> GetProfileImageResultAsync(string profilePath)
    {
        var localProfilePath = AppData.CombineWith(profilePath);
        var file = VirtualFiles.GetFile(localProfilePath);
        if (file != null)
        {
            return new HttpResult(file, MimeTypes.GetMimeType(file.Extension));
        }
        file = await r2.GetFileAsync(profilePath);
        var bytes = file != null ? await file.ReadAllBytesAsync() : null;
        if (bytes is { Length: > 0 })
        {
            await VirtualFiles.WriteFileAsync(localProfilePath, bytes);
            return new HttpResult(bytes, MimeTypes.GetMimeType(file!.Extension));
        }
        return new HttpResult(Svg.GetImage(Svg.Icons.Users), MimeTypes.ImageSvg);
    }

    public async Task<object> Any(GetProfileImage request)
    {
        var profilePath = "profiles".CombineWith(request.Path);
        return await GetProfileImageResultAsync(profilePath);
    }

    public async Task<object> Any(GetUserAvatar request)
    {
        if (!string.IsNullOrEmpty(request.UserName))
        {
            var profilePath = Db.Scalar<string>(Db.From<ApplicationUser>()
                .Where(x => x.UserName == request.UserName)
                .Select(x => x.ProfilePath));
            if (!string.IsNullOrEmpty(profilePath))
            {
                if (profilePath.StartsWith("data:"))
                {
                    var svg = imageCreator.DataUriToSvg(profilePath);
                    return new HttpResult(svg, MimeTypes.ImageSvg);
                }
                if (profilePath.StartsWith('/'))
                {
                    return await GetProfileImageResultAsync(profilePath);
                }
            }
        }
        return new HttpResult(Svg.GetImage(Svg.Icons.Users), MimeTypes.ImageSvg);
    }

    public object Any(UserPostData request)
    {
        var userName = Request.GetClaimsPrincipal().Identity!.Name!;
        var allUserPostVotes = Db.Select<Vote>(x => x.PostId == request.PostId && x.UserName == userName);

        var watchingPost = Db.Exists(Db.From<WatchPost>()
            .Where(x => x.PostId == request.PostId && x.UserName == userName && DateTime.UtcNow > x.AfterDate));
        var to = new UserPostDataResponse
        {
            Watching = watchingPost,
            QuestionsAsked = appConfig.GetQuestionCount(userName),
            UpVoteIds = allUserPostVotes.Where(x => x.Score > 0).Select(x => x.RefId).ToSet(),
            DownVoteIds = allUserPostVotes.Where(x => x.Score < 0).Select(x => x.RefId).ToSet(),
        };
        return to;
    }

    private string GetRequiredUserName()
    {
        var userName = Request.GetClaimsPrincipal().GetUserName()!;
        if (string.IsNullOrEmpty(userName))
            throw new ArgumentNullException(nameof(userName));
        return userName;
    }

    public async Task Any(PostVote request)
    {
        var userName = GetRequiredUserName();
        var postId = request.RefId.LeftPart('-').ToInt();
        var score = request.Up == true ? 1 : request.Down == true ? -1 : 0;
        
        var refUserName = request.RefId.IndexOf('-') >= 0
            ? request.RefId.RightPart('-')
            : Db.Scalar<string?>(Db.From<Post>().Where(x => x.Id == postId)
                .Select(x => x.CreatedBy));

        if (userName == refUserName)
            throw new ArgumentException("Can't vote on your own post", nameof(request.RefId));

        jobs.RunCommand<CreatePostVoteCommand>(new Vote {
            RefId = request.RefId,
            PostId = postId,
            UserName = userName,
            Score = score,
            RefUserName = refUserName,
        });
        jobs.RunCommand<UpdateReputationsCommand>();
    }

    public async Task Any(CommentVote request)
    {
        var userName = GetRequiredUserName();
        
        var postId = request.RefId.LeftPart('-').ToInt();
        var score = request.Up == true ? 1 : request.Down == true ? -1 : 0;
        
        jobs.RunCommand<CreateCommentVoteCommand>(new Vote {
            RefId = request.RefId,
            PostId = postId,
            UserName = userName,
            Score = score,
        });
        jobs.RunCommand<UpdateReputationsCommand>();
    }

    public object Any(CreateAvatar request)
    {
        var letter = char.ToUpper(request.UserName[0]);
        var svg = imageCreator.CreateSvg(letter, request.BgColor, request.TextColor);
        return new HttpResult(svg, MimeTypes.ImageSvg);
    }

    public async Task<object> Any(GetLatestNotifications request)
    {
        var userName = Request.GetClaimsPrincipal().GetUserName();
        var tuples = await Db.SelectMultiAsync<Notification,Post>(Db.From<Notification>()
            .Join<Post>()
            .Where(x => x.UserName == userName)
            .OrderByDescending(x => x.Id)
            .Take(30));

        Notification Merge(Notification notification, Post post)
        {
            notification.Title ??= post.Title.GenerateNotificationTitle();
            notification.Href ??= $"/questions/{notification.PostId}/{post.Slug}#{notification.RefId}";
            return notification;
        }
        
        var results = tuples.Map(x => Merge(x.Item1, x.Item2));
        
        return new GetLatestNotificationsResponse
        {
            HasUnread = appConfig.HasUnreadNotifications(userName),
            Results = results,
        };
    }
    
    public class SumAchievement
    {
        public int PostId { get; set; }
        public string RefId { get; set; }
        public int Score { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
    }

    public async Task<object> Any(GetLatestAchievements request)
    {
        var userName = Request.GetClaimsPrincipal().GetUserName();

        var sumAchievements = Db.Select<SumAchievement>(
            @"SELECT A.PostId, A.RefId, Sum(A.Score) AS Score, Max(A.CreatedDate) AS CreatedDate, P.Title, p.Slug 
                FROM Achievement A LEFT JOIN Post P on (A.PostId = P.Id)
               WHERE UserName = @userName
               GROUP BY A.PostId, A.RefId
               LIMIT 30", new { userName });

        var i = 0;
        var results = sumAchievements.Map(x => new Achievement
        {
            Id = ++i,
            PostId = x.PostId,
            RefId = x.RefId,
            Title = x.Title.GenerateNotificationTitle(),
            Score = x.Score,
            CreatedDate = x.CreatedDate,
            Href = $"/questions/{x.PostId}/{x.Slug}",
        });
        
        // Reset Achievements after every check
        appConfig.UsersUnreadAchievements[userName!] = 0;

        return new GetLatestAchievementsResponse
        {
            HasUnread = appConfig.HasUnreadAchievements(userName),
            Results = results
        };
    }

    public object Any(MarkAsRead request)
    {
        jobs.RunCommand<MarkAsReadCommand>(request, new() {
            UserId = Request.GetClaimsPrincipal().GetUserId()
        });
        return new EmptyResponse();
    }
    
    public object Any(GetUsersInfo request)
    {
        return new GetUsersInfoResponse
        {
            UsersQuestions = appConfig.UsersQuestions.ToDictionary(),
            UsersReputation = appConfig.UsersReputation.ToDictionary(),
            UsersUnreadAchievements = appConfig.UsersUnreadAchievements.ToDictionary(),
            UsersUnreadNotifications = appConfig.UsersUnreadNotifications.ToDictionary(),
        };
    }

    public async Task<object> Any(EnsureApplicationUser request)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                DisplayName = request.DisplayName,
                EmailConfirmed = request.EmailConfirmed ?? false,
                ProfilePath = request.ProfilePath,
                Model = request.Model,
            };
            await userManager!.CreateAsync(user, request.Password);
        }
        
        var newUser = await userManager.FindByEmailAsync(request.Email!);
        return new StringResponse
        {
            Result = newUser?.Id
        };
    }

    public async Task<object> Any(ShareContent request)
    {
        var postId = request.RefId.LeftPart('-').ToInt();
        var post = Db.SingleById<Post>(postId);
        if (post == null)
            return HttpResult.Redirect("/404");
        
        string? refUserName = request.UserId != null 
            ? Db.Scalar<string>("SELECT UserName FROM AspNetUsers WHERE ROWID = @UserId", new { request.UserId })
            : null;
        
        var userName = Request.GetClaimsPrincipal().GetUserName();
        jobs.RunCommand<AnalyticsTasksCommand>(new AnalyticsTasks {
            CreatePostStat = new()
            {
                PostId = postId,
                RefId = request.RefId,
                UserName = userName,
                RefUserName = refUserName,
                CreatedDate = DateTime.UtcNow,
                Type = PostStatType.Share,
                RemoteIp = Request?.RemoteIp,
            }
        });

        return HttpResult.Redirect($"/questions/{postId}/{post.Slug}" + (request.RefId.IndexOf('-') >= 0 ? $"#{request.RefId}" : ""));
    }

    public object Any(FlagContent request)
    {
        var postId = request.RefId.LeftPart('-').ToInt();
        var post = Db.SingleById<Post>(postId);
        if (post == null)
            return HttpError.NotFound("Does not exist");

        var userName = Request.GetClaimsPrincipal().GetUserName();
        lock (Locks.AppDb)
        {
            Db.Insert(new Flag
            {
                PostId = postId,
                RefId = request.RefId,
                UserName = userName,
                Type = request.Type,
                Reason = request.Reason,
                RemoteIp = Request?.RemoteIp,
                CreatedDate = DateTime.UtcNow,
            });
        }
        return new FlagContent();
    }
}
