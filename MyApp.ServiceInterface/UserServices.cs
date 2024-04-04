using MyApp.Data;
using ServiceStack;
using MyApp.ServiceModel;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using SixLabors.ImageSharp.Formats.Png;

namespace MyApp.ServiceInterface;

public class UserServices(R2VirtualFiles r2, ImageCreator imageCreator) : Service
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

    public async Task Any(PostVote request)
    {
        var userName = Request.GetClaimsPrincipal().GetUserName()!;
        if (string.IsNullOrEmpty(userName))
            throw new ArgumentNullException(nameof(userName));

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
            }
        });
    }

    public object Any(CreateAvatar request)
    {
        var letter = char.ToUpper(request.UserName[0]);
        var svg = imageCreator.CreateSvg(letter, request.BgColor, request.TextColor);
        return new HttpResult(svg, MimeTypes.ImageSvg);
    }
}
