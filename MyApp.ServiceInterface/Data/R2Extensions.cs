using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.IO;

namespace MyApp.Data;

public static class R2Extensions
{
    public static Dictionary<string,int> ModelScores = new()
    {
        ["starcoder2:3b"] = 1, //3B
        ["phi"] = 2, //2.7B
        ["gemma:2b"] = 3,
        ["gemma"] = 4, //7B
        ["codellama"] = 5, //7B
        ["mistral"] = 6, //7B
        ["starcoder2:15b"] = 7, //15B
        ["mixtral"] = 8, //47B
    };

    public static (string dir1, string dir2, string fileId) ToFileParts(this int id)
    {
        var idStr = $"{id}".PadLeft(9, '0');
        var dir1 = idStr[..3];
        var dir2 = idStr.Substring(3, 3);
        var fileId = idStr[6..];
        return (dir1, dir2, fileId);
    }

    public static async Task<IdFiles> GetQuestionFilesAsync(this R2VirtualFiles r2, int id)
    {
        var (dir1, dir2, fileId) = ToFileParts(id);
        
        var files = (await r2.EnumerateFilesAsync($"{dir1}/{dir2}").ToListAsync())
            .Where(x => x.Name.Glob($"{fileId}.*"))
            .OrderByDescending(x => x.LastModified)
            .Cast<IVirtualFile>()
            .ToList();
        
        return new IdFiles(id: id, dir1: dir1, dir2: dir2, fileId: fileId, files: files);
    }
    
    public static async Task<QuestionAndAnswers?> ToQuestionAndAnswers(this IdFiles idFiles)
    {
        var fileName = idFiles.FileId + ".json";
        await idFiles.LoadContentsAsync();

        var to = new QuestionAndAnswers();
        foreach (var entry in idFiles.FileContents)
        {
            if (entry.Key == fileName)
            {
                to.Post = entry.Value.FromJson<Post>();
            }
            else if (entry.Key.StartsWith(idFiles.FileId + ".a."))
            {
                to.Answers.Add(entry.Value.FromJson<Answer>());
            }
        }

        if (to.Post == null)
            return null;

        to.Answers.Each(x => x.UpVotes = ModelScores.GetValueOrDefault(x.Model, 1));
        to.Answers.Sort((a, b) => b.Votes - a.Votes);
        return to;
    }
}
