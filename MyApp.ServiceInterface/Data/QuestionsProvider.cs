using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.Messaging;

namespace MyApp.Data;

public class QuestionsProvider(ILogger<QuestionsProvider> log, IMessageProducer mqClient, IVirtualFiles fs, R2VirtualFiles r2)
{
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
}
