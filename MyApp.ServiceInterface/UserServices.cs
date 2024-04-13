using MyApp.Data;
using ServiceStack;
using MyApp.ServiceModel;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using SixLabors.ImageSharp.Formats.Png;

namespace MyApp.ServiceInterface;

public class UserServices(AppConfig appConfig, R2VirtualFiles r2, ImageCreator imageCreator) : Service
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

            await Db.UpdateOnlyAsync(() => new ApplicationUser {
                ProfilePath = profilePath,
            }, x => x.UserName == userName);
            
            PublishMessage(new DiskTasks {
                SaveFile = new() {
                    FilePath = origPath,
                    Stream = originalMs,
                }
            });
            PublishMessage(new DiskTasks {
                SaveFile = new() {
                    FilePath = profilePath,
                    Stream = resizedMs,
                }
            });
        }
        
        return new UpdateUserProfileResponse();
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
                    var localProfilePath = AppData.CombineWith(profilePath);
                    var file = VirtualFiles.GetFile(localProfilePath);
                    if (file != null)
                    {
                        return new HttpResult(file, MimeTypes.GetMimeType(file.Extension));
                    }
                    file = r2.GetFile(profilePath);
                    var bytes = file != null ? await file.ReadAllBytesAsync() : null;
                    if (bytes is { Length: > 0 })
                    {
                        await VirtualFiles.WriteFileAsync(localProfilePath, bytes);
                        return new HttpResult(bytes, MimeTypes.GetMimeType(file!.Extension));
                    }
                }
            }
        }
        return new HttpResult(Svg.GetImage(Svg.Icons.Users), MimeTypes.ImageSvg);
    }

    public async Task<object> Any(UserPostData request)
    {
        var userName = Request.GetClaimsPrincipal().Identity!.Name!;
        var allUserPostVotes = await Db.SelectAsync<Vote>(x => x.PostId == request.PostId && x.UserName == userName);
        
        var to = new UserPostDataResponse
        {
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
            : await Db.ScalarAsync<string?>(Db.From<Post>().Where(x => x.Id == postId)
                .Select(x => x.CreatedBy));

        if (userName == refUserName)
            throw new ArgumentException("Can't vote on your own post", nameof(request.RefId));
        
        MessageProducer.Publish(new DbWrites
        {
            CreatePostVote = new()
            {
                RefId = request.RefId,
                PostId = postId,
                UserName = userName,
                Score = score,
                RefUserName = refUserName,
            },
            UpdateReputations = new(),
        });
    }

    public async Task Any(CommentVote request)
    {
        var userName = GetRequiredUserName();
        
        var postId = request.RefId.LeftPart('-').ToInt();
        var score = request.Up == true ? 1 : request.Down == true ? -1 : 0;
        
        MessageProducer.Publish(new DbWrites
        {
            CreateCommentVote = new()
            {
                RefId = request.RefId,
                PostId = postId,
                UserName = userName,
                Score = score,
            },
            UpdateReputations = new(),
        });
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
            notification.Title ??= post.Title.SubstringWithEllipsis(0,100);
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

        var sumAchievements = await Db.SelectAsync<SumAchievement>(
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
            Title = x.Title.SubstringWithEllipsis(0,100),
            Score = x.Score,
            CreatedDate = x.CreatedDate,
            Href = $"/questions/{x.PostId}/{x.Slug}",
        });

        return new GetLatestAchievementsResponse
        {
            HasUnread = appConfig.HasUnreadAchievements(userName),
            Results = results
        };
    }

    public async Task<object> Any(MarkAsRead request)
    {
        request.UserName = Request.GetClaimsPrincipal().GetUserName()
            ?? throw new ArgumentNullException(nameof(MarkAsRead.UserName));
        MessageProducer.Publish(new DbWrites
        {
            MarkAsRead = request, 
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

    public async Task<object> Any(ShareContent request)
    {
        var postId = request.RefId.LeftPart('-').ToInt();
        var post = await Db.SingleByIdAsync<Post>(postId);
        if (post == null)
            return HttpResult.Redirect("/404");
        
        string? refUserName = request.UserId != null 
            ? await Db.ScalarAsync<string>("SELECT UserName FROM AspNetUsers WHERE ROWID = @UserId", new { request.UserId })
            : null;
        
        var userName = Request.GetClaimsPrincipal().GetUserName();
        MessageProducer.Publish(new AnalyticsTasks {
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

    public async Task<object> Any(FlagContent request)
    {
        var postId = request.RefId.LeftPart('-').ToInt();
        var post = await Db.SingleByIdAsync<Post>(postId);
        if (post == null)
            return HttpError.NotFound("Does not exist");

        var userName = Request.GetClaimsPrincipal().GetUserName();
        MessageProducer.Publish(new DbWrites {
            CreateFlag = new()
            {
                PostId = postId,
                RefId = request.RefId,
                UserName = userName,
                Type = request.Type,
                Reason = request.Reason,
                RemoteIp = Request?.RemoteIp,
                CreatedDate = DateTime.UtcNow,
            }
        });
        return new FlagContent();
    }
}
