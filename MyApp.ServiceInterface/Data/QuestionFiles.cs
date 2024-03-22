using System.Collections.Concurrent;
using System.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace MyApp.Data;

public class QuestionFiles(int id, string dir1, string dir2, string fileId, List<IVirtualFile> files, bool remote=false)
{
    public const int MostVotedScore = 10;
    public const int AcceptedScore = 9;
    public static Dictionary<string,int> ModelScores = new()
    {
        ["phi"] = 1, //2.7B
        ["gemma:2b"] = 2,
        ["qwen:4b"] = 3, //4B
        ["codellama"] = 4, //7B
        ["gemma"] = 5, //7B
        ["deepseek-coder:6.7b"] = 5, //6.7B
        ["mistral"] = 7, //7B
        ["mixtral"] = 8, //47B
        ["accepted"] = 9,
        ["most-voted"] = 10,
    };

    public int Id { get; init; } = id;
    public string Dir1 { get; init; } = dir1;
    public string Dir2 { get; init; } = dir2;
    public string DirPath = "/{Dir1}/{Dir2}";
    public string FileId { get; init; } = fileId;
    public List<IVirtualFile> Files { get; init; } = files;
    public bool LoadedRemotely { get; set; } = remote;
    public bool ScoresUpdated { get; set; }
    public ConcurrentDictionary<string, string> FileContents { get; } = [];
    public QuestionAndAnswers? Question { get; set; }
    public Meta? Meta { get; set; }

    public async Task<QuestionAndAnswers?> GetQuestionAsync()
    {
        if (Question == null)
        {
            await LoadQuestionAndAnswersAsync();
        }
        return Question;
    }
    
    public void UpdateScores(List<StatTotals> postStats)
    {
        if (Question == null)
            throw new ArgumentNullException(nameof(Question));
        if (ScoresUpdated)
            return;

        ScoresUpdated = true;

        var map = postStats.ToDictionary(x => x.Id);
        foreach (var answer in Question.Answers)
        {
            if (map.TryGetValue(answer.Id, out var stat))
            {
                answer.UpVotes = stat.UpVotes;
                answer.DownVotes = stat.DownVotes;
            }
        }
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

    public async Task LoadQuestionAndAnswersAsync()
    {
        var questionFileName = FileId + ".json";
        await LoadContentsAsync();

        var to = new QuestionAndAnswers();
        foreach (var entry in FileContents)
        {
            var fileName = entry.Key.LastRightPart('/');
            if (fileName == questionFileName)
            {
                to.Post = entry.Value.FromJson<Post>();
            }
            else if (fileName == $"{FileId}.meta.json")
            {
                Meta = entry.Value.FromJson<Meta>();
                Meta.StatTotals ??= new();
                Meta.ModelVotes ??= new();
            }
            else if (fileName.StartsWith(FileId + ".a."))
            {
                var answer = entry.Value.FromJson<Answer>();
                answer.Id = $"{Id}-{answer.Model.Replace(':','-')}";
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
                    UpVotes = userName == "most-voted" ? MostVotedScore : AcceptedScore,
                    Choices = [
                        new()
                        {
                            Index = 1,
                            Message = new() { Role = userName, Content = post.Body ?? "" }
                        }
                    ]
                };
                if (to.Answers.All(x => x.Id != answer.Id))
                    to.Answers.Add(answer);
            }
        }

        if (to.Post == null)
            return;

        to.Answers.Each(x => x.UpVotes = x.UpVotes == 0 ? ModelScores.GetValueOrDefault(x.Model, 1) : x.UpVotes);
        to.Answers.Sort((a, b) => b.Votes - a.Votes);

        if (Meta?.StatTotals.Count > 0)
        {
            UpdateScores(Meta.StatTotals);
        }
        
        Question = to;
    }
}
