using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class LeaderboardServices : Service
{
    
    private const int minimumQuestions = 50;
    /// <summary>
    /// Leader board stats
    /// - Most liked models by human votes
    /// - Most liked models by LLM votes
    /// - Answerer win rate
    /// - Human vs LLM win rate by human votes
    /// - Human vs LLM win rate by LLM votes
    /// - Model win rate by tag
    /// - Model total score
    /// - Model total score by tag
    /// </summary>
    /// <param name="request"></param>
    public async Task<object> Any(CalculateLeaderBoard request)
    {
        var statTotals = await Db.SelectAsync<StatTotals>();
        var modelsToExclude = request.ModelsToExclude?.Split(",").ToList() ?? new List<string>();
        // filter to answers only
        var answers = statTotals.Where(x => FilterSpecificModels(x, modelsToExclude)).ToList();
        // Sum up votes by model, first group by UserName
        var statsByUser = answers.GroupBy(x => x.Id.RightPart('-')).Select(x => new StatTotals
        {
            Id = x.Key,
            UpVotes = x.Sum(y => y.UpVotes),
            DownVotes = x.Sum(y => y.DownVotes),
            StartingUpVotes = x.Sum(y => y.StartingUpVotes),
            FavoriteCount = x.Sum(y => y.FavoriteCount)
        }).ToList();
        
        var leaderBoard = CalculateLeaderboardResponse(statsByUser, answers);

        leaderBoard.AnswererWinRate = leaderBoard.AnswererWinRate
            .Where(x => x.NumberOfQuestions > minimumQuestions).ToList();
        leaderBoard.ModelWinRate = leaderBoard.ModelWinRate
            .Where(x => x.NumberOfQuestions > minimumQuestions).ToList();
        leaderBoard.ModelTotalScore = leaderBoard.ModelTotalScore
            .Where(x => x.TotalScore > 500).ToList();
        leaderBoard.MostLikedModelsByLlm = leaderBoard.MostLikedModelsByLlm
            .Where(x => x.StartingUpVotes > 500).ToList();
        
        // Serialize the response to a leaderboard json file
        var json = leaderBoard.ToJson();
        var modelsToExcludeSlug = request.ModelsToExclude?.GenerateSlug();
        var combinedSuffix = modelsToExcludeSlug.IsNullOrEmpty() ? "" : $"-{modelsToExcludeSlug}";
        await File.WriteAllTextAsync($"App_Data/leaderboard{combinedSuffix}.json", json);
        
        return leaderBoard;
    }

    public async Task<object> Any(CalculateTop1KLeaderboard request)
    {
        // Do the same for top 1000 questions
        var topQuestions = await Db.SelectAsync(Db.From<Post>().OrderByDescending(x => x.Score).Limit(1000));
        var postIds = topQuestions.Select(x => x.Id).ToList();
        
        var statTotals = await Db.SelectAsync(Db.From<StatTotals>()
            .Where(x => Sql.In(x.PostId,postIds)));
        
        // filter to answers only
        var answers = statTotals.Where(x => FilterSpecificModels(x)).ToList();
        
        var topStatsByUser = answers.GroupBy(x => x.Id.RightPart('-')).Select(x => new StatTotals
        {
            Id = x.Key,
            UpVotes = x.Sum(y => y.UpVotes),
            DownVotes = x.Sum(y => y.DownVotes),
            StartingUpVotes = x.Sum(y => y.StartingUpVotes),
            FavoriteCount = x.Sum(y => y.FavoriteCount)
        }).ToList();
        
        var topLeaderBoard = CalculateLeaderboardResponse(topStatsByUser, statTotals);
        
        topLeaderBoard.AnswererWinRate = topLeaderBoard.AnswererWinRate
            .Where(x => x.NumberOfQuestions > minimumQuestions).ToList();
        topLeaderBoard.ModelWinRate = topLeaderBoard.ModelWinRate
            .Where(x => x.NumberOfQuestions > minimumQuestions).ToList();
        topLeaderBoard.ModelTotalScore = topLeaderBoard.ModelTotalScore
            .Where(x => x.TotalScore > 500).ToList();
        topLeaderBoard.MostLikedModelsByLlm = topLeaderBoard.MostLikedModelsByLlm
            .Where(x => x.StartingUpVotes > 500).ToList();
        
        var topJson = topLeaderBoard.ToJson();
        var modelsToExcludeSlug = request.ModelsToExclude?.GenerateSlug();
        var combinedSuffix = modelsToExcludeSlug.IsNullOrEmpty() ? "" : $"-{modelsToExcludeSlug}";
        await File.WriteAllTextAsync($"App_Data/leaderboard-top1000{combinedSuffix}.json", topJson);

        return topLeaderBoard;
    }

    private static bool FilterSpecificModels(StatTotals x,List<string>? modelsToExclude = null)
    {
        var excludedModels = modelsToExclude ?? [];
        return x.Id.Contains('-') 
               && !x.Id.EndsWith("-accepted") 
               && !x.Id.EndsWith("-most-voted")
               && !x.Id.EndsWith("-undefined")
               && !excludedModels.Contains(x.Id.RightPart('-'));
    }

    private CalculateLeaderboardResponse CalculateLeaderboardResponse(List<StatTotals> statsByUser, List<StatTotals> answers)
    {
        var overallWinRates = statsByUser.GroupBy(x => x.Id).Select(y =>
        {
            var id = "-" + y.Key;
            // sum all the wins for this user
            var res = new LeaderBoardWinRate
            {
                Id = y.Key,
                WinRate = CalculateWinRate(answers, y.Key),
                NumberOfQuestions = answers.Count(x => x.Id.EndsWith(id))
            };
            return res;
        }).ToList();

        var leaderBoard = new CalculateLeaderboardResponse
        {
            MostLikedModelsByLlm = statsByUser.Where(x => IsHuman(x.Id) == false)
                .OrderByDescending(x => x.StartingUpVotes)
                .Select(x => new ModelTotalStartUpVotes
                {
                    Id = x.Id,
                    StartingUpVotes = x.GetScore()
                })
                .ToList(),
            
            AnswererWinRate = overallWinRates,
            ModelWinRate = overallWinRates.Where(x => IsHuman(x.Id) == false)
                .Select(x => new ModelWinRate
                {
                    Id = x.Id,
                    WinRate = x.WinRate * 100,
                    NumberOfQuestions = x.NumberOfQuestions
                }).ToList(),
            ModelTotalScore = statsByUser.GroupBy(x => x.Id)
                .Select(x => new ModelTotalScore
                {
                    Id = x.Key,
                    TotalScore = x.Sum(y => y.GetScore())
                }).ToList()
        };
        return leaderBoard;
    }

    bool IsHuman(string id) => id is "accepted" or "most-voted";


    /// <summary>
    /// Take all answers, group by PostId derived user, select the answer with the highest score
    /// all divided by the total question count
    /// </summary>
    /// <param name="answers"></param>
    /// <param name="name"></param>
    /// <param name="modelsToExclude"></param>
    /// <param name="questionCount"></param>
    /// <returns></returns>
    double CalculateWinRate(List<StatTotals> answers, string name)
    {
        var questionsIncluded = answers.Where(x => x.Id.EndsWith("-" + name)).Select(x => x.PostId).Distinct().ToList();
        var questionsAnswered = questionsIncluded.Count;
        if (questionsAnswered == 0)
        {
            return 0;
        }
        
        // Create a dictionary to store user scores
        var userScoreDict = answers.Where(x => x.Id.EndsWith("-" + name)).ToDictionary(y => $"{y.PostId}-{name}", y => y.GetScore());

        double winRate = answers
            .Where(x => questionsIncluded.Contains(x.PostId))
            .GroupBy(x => x.PostId)
            .Select(g => new
            {
                PostId = g.Key,
                TopScores = g.GroupBy(x => x.GetScore())
                    .OrderByDescending(x => x.Key)
                    .Take(2)
                    .Select(x => new { Score = x.Key, Count = x.Count() })
                    .ToList()
            })
            .Select(x =>
            {
                // Check if the user score exists in the dictionary
                if (userScoreDict.TryGetValue($"{x.PostId}-{name}", out var userScore))
                {
                    return x.TopScores[0].Count > 1 && x.TopScores[0].Score == userScore
                           || x.TopScores[0].Count == 1 && x.TopScores[0].Score == userScore;
                }
                return false;
            })
            .Count(x => x);

        var totalQuestionsAnswered = answers
            .Where(x => questionsIncluded.Contains(x.PostId) && x.Id.Contains($"-{name}"))
            .Select(x => x.PostId)
            .Distinct()
            .Count();

        winRate = totalQuestionsAnswered > 0 ? winRate / (double)totalQuestionsAnswered : 0;
        
        return winRate;
    }

    public async Task<object> Any(GetLeaderboardStatsByTag request)
    {
        var allStatsForTag = await Db.SelectAsync<StatTotals>(@"SELECT st.*
                FROM main.StatTotals st WHERE st.PostId in 
                    (SELECT Id 
                       FROM post p 
                      WHERE p.Tags LIKE @TagMiddle OR p.Tags LIKE @TagLeft OR 
                            p.Tags LIKE @TagRight OR p.Tags = @TagSolo)", 
                new { TagSolo = $"[{request.Tag}]", 
            TagRight = $"%,{request.Tag}]", 
            TagLeft = $"[{request.Tag},%",
            TagMiddle = $"%,{request.Tag},%",
        });
        var modelsToExclude = request.ModelsToExclude?.Split(",").ToList() ?? new List<string>();
        // filter to answers only
        var answers = allStatsForTag.Where(x => FilterSpecificModels(x,modelsToExclude)).ToList();
        // Sum up votes by model, first group by UserName
        var statsByUser = answers.GroupBy(x => x.Id.RightPart('-')).Select(x => new StatTotals
        {
            Id = x.Key,
            UpVotes = x.Sum(y => y.UpVotes),
            DownVotes = x.Sum(y => y.DownVotes),
            StartingUpVotes = x.Sum(y => y.StartingUpVotes),
            FavoriteCount = x.Sum(y => y.FavoriteCount)
        }).ToList();

        var result = CalculateLeaderboardResponse(statsByUser,answers);

        // Serialize the response to a leaderboard json file
        var json = result.ToJson();
        // Filter to only filename safe characters
        request.Tag = request.Tag.GenerateSlug();
        var modelToExclude = request.ModelsToExclude?.GenerateSlug();
        var combinedSuffix = modelToExclude.IsNullOrEmpty() ? "" : $"-{modelToExclude}";
        await File.WriteAllTextAsync($"App_Data/leaderboard-tag-{request.Tag}{combinedSuffix}.json", json);
        
        return result;
    }
}

public class GetLeaderboardStatsHuman
{
}

public class GetLeaderboardStatsByTag
{
    public string Tag { get; set; }
    public string? ModelsToExclude { get; set; }
}

public class CalculateLeaderboardResponse
{
    public List<ModelTotalStartUpVotes> MostLikedModelsByLlm { get; set; }
    public List<LeaderBoardWinRate> AnswererWinRate { get; set; }
    public List<ModelTotalScore> ModelTotalScore { get; set; }
    public List<ModelWinRate> ModelWinRate { get; set; }
}

public class ModelTotalScoreByTag
{
    public string Id { get; set; }
    public string Tag { get; set; }
    public int TotalScore { get; set; }
}

public class ModelTotalScore
{
    public string Id { get; set; }
    public int TotalScore { get; set; }
}

public class ModelTotalStartUpVotes
{
    public string Id { get; set; }
    public int StartingUpVotes { get; set; }
}

public class ModelWinRateByTag
{
    public string Id { get; set; }
    public string Tag { get; set; }
    public double WinRate { get; set; }
}

public class ModelWinRate
{
    public string Id { get; set; }
    public double WinRate { get; set; }
    
    public int NumberOfQuestions { get; set; }
}

public class LeaderBoardWinRate
{
    public string Id { get; set; }
    public double WinRate { get; set; }
    
    public int NumberOfQuestions { get; set; }
}

public record LeaderboardStat
{
    public int Rank { get; set; }
    public string AvatarUrl { get; set; }
    public string DisplayName { get; set; }
    public string Stat { get; set; }
    public double Value { get; set; }
}

public class CalculateLeaderBoard : IReturn<CalculateLeaderboardResponse>, IGet
{
    public string? ModelsToExclude { get; set; }
}

public class CalculateTop1KLeaderboard : IReturn<CalculateLeaderboardResponse>, IGet
{
    public string? ModelsToExclude { get; set; }
}