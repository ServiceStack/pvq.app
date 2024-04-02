using Microsoft.Extensions.Logging;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.Text;

namespace MyApp.Data;

public class QuestionsProvider(ILogger<QuestionsProvider> log, IVirtualFiles fs, R2VirtualFiles r2)
{
    public const int MostVotedScore = 10;
    public const int AcceptedScore = 9;
    public static List<string> ModelUserNames { get; } = [
        "phi", "gemma-2b", "qwen-4b", "codellama", "gemma", "deepseek-coder-6.7b", "mistral", "mixtral","gpt-4-turbo",
        "claude-3-haiku","claude-3-sonnet","claude-3-opus"
    ];
    public static Dictionary<string,int> ModelScores = new()
    {
        ["phi"] = 1, //2.7B
        ["gemma:2b"] = 2,
        ["qwen:4b"] = 3, //4B
        ["codellama"] = 4, //7B
        ["gemma"] = 5, //7B
        ["deepseek-coder:6.7b"] = 5, //6.7B
        ["deepseek-coder:6"] = 5, //TODO Remove once data is clean, some 6.7b models are saved as 6 due to finding the first decimal
        ["deepseek-coder:33b"] = 6, //33B
        ["mistral"] = 7, //7B
        ["mixtral"] = 8, //47B
        ["accepted"] = 9,
        ["most-voted"] = 10,
    };

    public System.Text.Json.JsonSerializerOptions SystemJsonOptions = new(TextConfig.SystemJsonOptions)
    {
        WriteIndented = true
    };

    public string ToJson<T>(T obj) => System.Text.Json.JsonSerializer.Serialize(obj, SystemJsonOptions); 
    
    public QuestionFiles GetLocalQuestionFiles(int id)
    {
        var (dir1, dir2, fileId) = id.ToFileParts();
        
        var files = fs.GetDirectory($"{dir1}/{dir2}").GetAllMatchingFiles($"{fileId}.*")
            .OrderByDescending(x => x.LastModified)
            .ToList();
        
        return new QuestionFiles(id: id, dir1: dir1, dir2: dir2, fileId: fileId, files: files);
    }

    public async Task<QuestionFiles> GetRemoteQuestionFilesAsync(int id)
    {
        var (dir1, dir2, fileId) = id.ToFileParts();
        
        var files = (await r2.EnumerateFilesAsync($"{dir1}/{dir2}").ToListAsync())
            .Where(x => x.Name.Glob($"{fileId}.*"))
            .OrderByDescending(x => x.LastModified)
            .Cast<IVirtualFile>()
            .ToList();
        
        return new QuestionFiles(id: id, dir1: dir1, dir2: dir2, fileId: fileId, files: files, remote:true);
    }

    public static string GetQuestionPath(int id)
    {
        var (dir1, dir2, fileId) = id.ToFileParts();
        var path = $"{dir1}/{dir2}/{fileId}.json";
        return path;
    }

    public static string GetModelAnswerPath(int id, string model)
    {
        var (dir1, dir2, fileId) = id.ToFileParts();
        var path = $"{dir1}/{dir2}/{fileId}.a.{model.Replace(':','-')}.json";
        return path;
    }

    public static string GetHumanAnswerPath(int id, string userName)
    {
        var (dir1, dir2, fileId) = id.ToFileParts();
        var path = $"{dir1}/{dir2}/{fileId}.h.{userName}.json";
        return path;
    }

    public static string GetMetaPath(int id)
    {
        var (dir1, dir2, fileId) = id.ToFileParts();
        var path = $"{dir1}/{dir2}/{fileId}.meta.json";
        return path;
    }
    
    public async Task SaveFileAsync(string virtualPath, string contents)
    {
        await Task.WhenAll(
            r2.WriteFileAsync(virtualPath, contents),
            fs.WriteFileAsync(virtualPath, contents));
    }
    
    public async Task DeleteFileAsync(string virtualPath)
    {
        fs.DeleteFile(virtualPath);
        await r2.DeleteFileAsync(virtualPath);
    }

    public async Task WriteMetaAsync(Meta meta)
    {
        await SaveFileAsync(GetMetaPath(meta.Id), ToJson(meta));
    }

    public async Task<QuestionFiles> GetQuestionFilesAsync(int id)
    {
        var localFiles = GetLocalQuestionFiles(id);
        if (localFiles.Files.Count > 0)
            return localFiles;

        log.LogInformation("No local cached files for question {Id}, fetching from R2...", id);
        var r = await GetRemoteQuestionFilesAsync(id);
        if (r.Files.Count > 0)
        {
            var lastModified = r.Files.Max(x => x.LastModified);
            log.LogInformation("Fetched {Count} files from R2 for question {Id}, last modified: '{LastModified}'", r.Files.Count, id, lastModified);
        }
        return r;
    }

    public async Task<QuestionFiles> GetQuestionAsync(int id)
    {
        var questionFiles = await GetQuestionFilesAsync(id);
        await questionFiles.GetQuestionAsync();
        if (questionFiles.LoadedRemotely)
        {
            log.LogInformation("Caching question {Id}'s {Count} remote files locally...", id, questionFiles.FileContents.Count);
            foreach (var entry in questionFiles.FileContents)
            {
                await fs.WriteFileAsync(entry.Key, entry.Value);
            }
        }
        return questionFiles;
    }

    public async Task<IVirtualFile?> GetAnswerFileAsync(string refId)
    {
        if (refId.IndexOf('-') < 0)
            throw new ArgumentException("Invalid Answer Id", nameof(refId));
        
        var postId = refId.LeftPart('-').ToInt();
        var userName = refId.RightPart('-');
        var answerPath = ModelUserNames.Contains(userName)
            ? GetModelAnswerPath(postId, userName)
            : GetHumanAnswerPath(postId, userName);

        var file = fs.GetFile(answerPath)
                ?? r2.GetFile(answerPath);

        if (file == null)
        {
            // After first edit AI Model is converted to h. (Post) answer
            var modelAnswerPath = GetHumanAnswerPath(postId, userName);
            file = fs.GetFile(modelAnswerPath)
                   ?? r2.GetFile(modelAnswerPath);
        }
        
        return file;
    }

    public async Task SaveQuestionAsync(Post post)
    {
        await SaveFileAsync(GetQuestionPath(post.Id), ToJson(post));
    }

    public async Task SaveModelAnswerAsync(int postId, string model, string json)
    {
        await SaveFileAsync(GetModelAnswerPath(postId, model), json);
    }

    public async Task SaveHumanAnswerAsync(Post post)
    {
        await SaveFileAsync(GetHumanAnswerPath(
            post.ParentId ?? throw new ArgumentNullException(nameof(Post.ParentId)), 
            post.CreatedBy ?? throw new ArgumentNullException(nameof(Post.CreatedBy))), 
            ToJson(post));
    }

    public async Task SaveHumanAnswerAsync(int postId, string userName, string json)
    {
        await SaveFileAsync(GetHumanAnswerPath(postId, userName), json);
    }

    public async Task SaveLocalFileAsync(string virtualPath, string contents)
    {
        await fs.WriteFileAsync(virtualPath, contents);
    }
    
    public async Task SaveRemoteFileAsync(string virtualPath, string contents)
    {
        await r2.WriteFileAsync(virtualPath, contents);
    }
    
    public async Task<IVirtualFile?> GetQuestionFileAsync(int id)
    {
        var (dir1, dir2, fileId) = id.ToFileParts();
        var questionPath = $"{dir1}/{dir2}/{fileId}.json";
        var file = fs.GetFile(questionPath)
                   ?? await r2.GetFileAsync(questionPath);
        return file;
    }

    public async Task<IVirtualFile?> GetMetaFileAsync(int id)
    {
        var (dir1, dir2, fileId) = id.ToFileParts();
        var metaPath = $"{dir1}/{dir2}/{fileId}.meta.json";
        var file = fs.GetFile(metaPath)
                   ?? await r2.GetFileAsync(metaPath);
        return file;
    }

    public async Task<Meta> GetMetaAsync(int id)
    {
        var metaFile = await GetMetaFileAsync(id);
        var metaJson = metaFile != null
            ? await metaFile.ReadAllTextAsync()
            : "{}";
        
        var meta = metaJson.FromJson<Meta>();
        return meta;
    }

    public async Task SaveMetaFileAsync(int id, string metaJson)
    {
        var (dir1, dir2, fileId) = id.ToFileParts();
        var metaPath = $"{dir1}/{dir2}/{fileId}.meta.json";
        await SaveFileAsync(metaPath, metaJson);
    }

    public async Task SaveMetaAsync(int postId, Meta meta)
    {
        var updatedJson = ToJson(meta);
        await SaveMetaFileAsync(postId, updatedJson);
    }

    public async Task SaveQuestionEditAsync(Post question)
    {
        var questionFile = await GetQuestionFileAsync(question.Id);
        if (questionFile == null)
            throw new FileNotFoundException($"Question {question.Id} does not exist", GetQuestionPath(question.Id));
        var existingQuestionJson = await questionFile.ReadAllTextAsync();
        var existingQuestion = existingQuestionJson.FromJson<Post>();

        var (dir1, dir2, fileId) = question.Id.ToFileParts();
        var tasks = new List<Task>();

        if (existingQuestion.ModifiedBy == null || question.ModifiedBy != existingQuestion.ModifiedBy)
        {
            var datePart = question.LastEditDate!.Value.ToString("yyMMdd-HHmmss");
            var editFile = "edit.q." + question.Id + "-" + question.ModifiedBy + "_" + datePart + ".json";
            var editFilePath = $"{dir1}/{dir2}/{editFile}";
            tasks.Add(SaveFileAsync(editFilePath, existingQuestionJson));
        }

        tasks.Add(SaveQuestionAsync(question));
        await Task.WhenAll(tasks);
    }

    public async Task SaveAnswerEditAsync(IVirtualFile existingAnswer, string userName, string body, string editReason)
    {
        var now = DateTime.UtcNow;
        var existingAnswerJson = await existingAnswer.ReadAllTextAsync();
        var tasks = new List<Task>();

        var fileName = existingAnswer.VirtualPath.TrimStart('/').Replace("/", "");
        var postId = fileName.LeftPart('.').ToInt();
        string existingAnswerBy = "";
        var newAnswer = new Post
        {
            Id = postId,
        };
        
        if (fileName.Contains(".a."))
        {
            existingAnswerBy = fileName.RightPart(".a.").LastLeftPart('.');
            var datePart = DateTime.UtcNow.ToString("yyMMdd-HHmmss");
            var editFilePath = existingAnswer.VirtualPath.LastLeftPart('/') + "/edit.a." + postId + "-" + userName + "_" + datePart + ".json";
            tasks.Add(SaveFileAsync(editFilePath, existingAnswerJson));
            tasks.Add(DeleteFileAsync(existingAnswer.VirtualPath));

            var obj = (Dictionary<string,object>)JSON.parse(existingAnswerJson);
            newAnswer.CreationDate = obj.TryGetValue("created", out var oCreated) && oCreated is int created
                ? DateTimeOffset.FromUnixTimeSeconds(created).DateTime
                : existingAnswer.LastModified;
        }
        else if (fileName.Contains(".h."))
        {
            existingAnswerBy = fileName.RightPart(".h.").LastLeftPart('.');
            var datePart = DateTime.UtcNow.ToString("yyMMdd-HHmmss");
            newAnswer = existingAnswerJson.FromJson<Post>();
            
            // Just override the existing answer if it's the same user 
            if (newAnswer.ModifiedBy != userName)
            {
                var editFilePath = existingAnswer.VirtualPath.LastLeftPart('/') + "/edit.h." + postId + "-" + userName + "_" + datePart + ".json";
                tasks.Add(SaveFileAsync(editFilePath, existingAnswerJson));
            }
        }
        else throw new ArgumentException($"Invalid Answer File {existingAnswer.Name}", nameof(existingAnswer));

        newAnswer.Body = body;
        newAnswer.CreatedBy ??= existingAnswerBy;
        newAnswer.ModifiedBy = userName;
        newAnswer.LastEditDate = now;
        newAnswer.ModifiedReason = editReason;

        var newFileName = $"{existingAnswer.Name.LeftPart('.')}.h.{existingAnswerBy}.json";
        var newFilePath = existingAnswer.VirtualPath.LastLeftPart('/') + "/" + newFileName;
        tasks.Add(SaveFileAsync(newFilePath, ToJson(newAnswer)));
        
        await Task.WhenAll(tasks);
    }

    public async Task DeleteQuestionFilesAsync(int id)
    {
        var localQuestionFiles = GetLocalQuestionFiles(id);
        fs.DeleteFiles(localQuestionFiles.Files.Select(x => x.VirtualPath));
        var remoteQuestionFiles = await GetRemoteQuestionFilesAsync(id);
        r2.DeleteFiles(remoteQuestionFiles.Files.Select(x => x.VirtualPath));
    }
}

// TODO: Use Native Methods on S3VirtualFiles in vNext
public static class R2VirtualFilesExtensions
{
    public static ServiceStack.Aws.S3.S3VirtualDirectory? GetParentDirectory(R2VirtualFiles r2, string dirPath)
    {
        if (string.IsNullOrEmpty(dirPath))
            return null;
        
        var parentDirPath = r2.GetDirPath(dirPath.TrimEnd(S3VirtualFiles.DirSep));
        var parentDir = parentDirPath != null
            ? GetParentDirectory(r2, parentDirPath)
            : (ServiceStack.Aws.S3.S3VirtualDirectory)r2.RootDirectory;
        return new ServiceStack.Aws.S3.S3VirtualDirectory(r2, dirPath, parentDir);
    }

    public static async Task<IVirtualFile?> GetFileAsync(this R2VirtualFiles r2, string virtualPath)
    {
        if (string.IsNullOrEmpty(virtualPath))
            return null;

        var filePath = r2.SanitizePath(virtualPath);
        try
        {
            var response = await r2.AmazonS3.GetObjectAsync(new Amazon.S3.Model.GetObjectRequest
            {
                Key = filePath,
                BucketName = r2.BucketName,
            }).ConfigAwait();

            var dirPath = r2.GetDirPath(filePath);
            var dir = dirPath == null
                ? r2.RootDirectory
                : GetParentDirectory(r2, dirPath);
            return new ServiceStack.Aws.S3.S3VirtualFile(r2, dir).Init(response);
        }
        catch (Amazon.S3.AmazonS3Exception ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            throw;
        }
    }

    public static async Task DeleteFileAsync(this R2VirtualFiles r2, string virtualPath)
    {
        await r2.AmazonS3.DeleteObjectAsync(new Amazon.S3.Model.DeleteObjectRequest
        {
            BucketName = r2.BucketName,
            Key = r2.SanitizePath(virtualPath),
        });
    }
}
