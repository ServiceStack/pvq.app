using System.Collections.Concurrent;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.Text;

namespace MyApp.Data;

public class QuestionFiles(int id, string dir1, string dir2, string fileId, List<IVirtualFile> files, bool remote=false)
{
    public int GetModelScore(string model) => QuestionsProvider.ModelScores.GetValueOrDefault(model, 0);

    public int Id { get; init; } = id;
    public string Dir1 { get; init; } = dir1;
    public string Dir2 { get; init; } = dir2;
    public string DirPath = "/{Dir1}/{Dir2}";
    public string FileId { get; init; } = fileId;
    public List<IVirtualFile> Files { get; init; } = WithoutDuplicateAnswers(files);
    public bool LoadedRemotely { get; set; } = remote;
    public ConcurrentDictionary<string, string> FileContents { get; } = [];
    public QuestionAndAnswers? Question { get; set; }

    public static Meta DeserializeMeta(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new Meta();
        
        var meta = json.FromJson<Meta>();
        var toRemove = new List<string>();
        foreach (var item in meta.Comments)
        {
            if (item.Key.Contains('[') || item.Key.Contains(']'))
            {
                toRemove.Add(item.Key);
            }
        }
        toRemove.ForEach(key => meta.Comments.Remove(key));
        return meta;
    }

    public IVirtualFile? GetQuestionFile() => Files.FirstOrDefault(x => x.Name == $"{FileId}.json");
    public IVirtualFile? GetMetaFile() => Files.FirstOrDefault(x => x.Name == $"{FileId}.meta.json");
    
    public static List<IVirtualFile> WithoutDuplicateAnswers(List<IVirtualFile> files)
    {
        var accepted = files.FirstOrDefault(x => x.Name.Contains(".h.accepted"));
        var mostVoted = files.FirstOrDefault(x => x.Name.Contains(".h.most-voted"));
        return accepted?.Length == mostVoted?.Length
            ? files.Where(x => !Equals(x, accepted)).ToList()
            : files;
    }
    
    public IEnumerable<IVirtualFile> GetAnswerFiles() => Files.Where(x => x.Name.Contains(".a.") || x.Name.Contains(".h."));

    public int GetAnswerFilesCount() => GetAnswerFiles().Count();

    public async Task<QuestionAndAnswers?> GetQuestionAsync()
    {
        if (Question == null)
        {
            await LoadQuestionAndAnswersAsync();
        }
        return Question;
    }
    
    public void ApplyScores(List<StatTotals> postStats)
    {
        if (Question == null)
            throw new ArgumentNullException(nameof(Question));
        
        Question.Answers.Sort((a, b) =>
        {
            var aScore = postStats.FirstOrDefault(x => x.Id == a.Id)?.GetScore()
                         ?? 0;
            var bScore = postStats.FirstOrDefault(x => x.Id == b.Id)?.GetScore()
                         ?? 0;
            return bScore - aScore;
        });
    }
    
    public async Task LoadContentsAsync()
    {
        if (FileContents.Count > 0) return;
        var tasks = new List<Task>();
        tasks.AddRange(Files.Select(async file => {
            FileContents[file.VirtualPath] = await file.ReadAllTextAsync();
        }));
        await Task.WhenAll(tasks);
    }

    public string GetAnswerUserName(string answerFileName) => answerFileName[(FileId + ".a.").Length..].LastLeftPart('.');

    public string GetAnswerId(string answerFileName) => Id + "-" + GetAnswerUserName(answerFileName);

    public async Task LoadQuestionAndAnswersAsync()
    {
        var questionFileName = FileId + ".json";
        await LoadContentsAsync();
        
        var to = new QuestionAndAnswers();
        foreach (var entry in FileContents)
        {
            if (string.IsNullOrEmpty(entry.Value))
                continue;
            
            var fileName = entry.Key.LastRightPart('/');
            if (fileName == questionFileName)
            {
                to.Post = entry.Value.FromJson<Post>();
            }
            else if (fileName == $"{FileId}.meta.json")
            {
                to.Meta = DeserializeMeta(entry.Value);
                to.Meta.StatTotals ??= new();
                to.Meta.ModelVotes ??= new();
            }
            else if (fileName.StartsWith(FileId + ".a."))
            {
                var answer = entry.Value.FromJson<Answer>();
                answer.Id = GetAnswerId(fileName);
                to.Answers.Add(answer);
            }
            else if (fileName.StartsWith(FileId + ".h."))
            {
                var post = entry.Value.FromJson<Post>();
                var userName = fileName.Substring((FileId + ".h.").Length).LeftPart('.');
                var answer = new Answer
                {
                    Id = $"{Id}-{userName}",
                    Model = userName,
                    Created = (post.LastEditDate ?? post.CreationDate).ToUnixTime(),
                    Choices = [
                        new()
                        {
                            Index = 1,
                            Message = new() { Role = userName, Content = post.Body ?? "" }
                        }
                    ]
                };
                to.Answers.Add(answer);
            }
        }

        if (to.Post == null)
            return;
        
        Question = to;

        if (to.Meta?.StatTotals.Count > 0)
        {
            ApplyScores(to.Meta.StatTotals);
        }
    }
}
