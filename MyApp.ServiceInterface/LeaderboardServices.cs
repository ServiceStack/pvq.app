using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class LeaderboardServices : Service
{
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
        // filter to answers only
        var answers = statTotals.Where(x => x.Id.Contains('-') 
                                            && !x.Id.Contains("-accepted") 
                                            && !x.Id.Contains("-most-voted")
                                            && !x.Id.Contains("-undefined")
                                            ).ToList();
        // Sum up votes by model, first group by UserName
        var statsByUser = answers.GroupBy(x => x.Id.SplitOnFirst('-')[1]).Select(x => new StatTotals
        {
            Id = x.Key,
            UpVotes = x.Sum(y => y.UpVotes),
            DownVotes = x.Sum(y => y.DownVotes),
            StartingUpVotes = x.Sum(y => y.StartingUpVotes),
            FavoriteCount = x.Sum(y => y.FavoriteCount)
        }).ToList();
        
        var leaderBoard = CalculateLeaderboardResponse(statsByUser, answers);
        
        // Serialize the response to a leaderboard json file
        var json = leaderBoard.ToJson();
        await File.WriteAllTextAsync("App_Data/leaderboard.json", json);
        
        return leaderBoard;
    }

    private CalculateLeaderboardResponse CalculateLeaderboardResponse(List<StatTotals> statsByUser, List<StatTotals> answers)
    {


        var overallWinRates = statsByUser.GroupBy(x => x.Id).Select(y =>
        {
            // sum all the wins for this user
            var res = new LeaderBoardWinRate
            {
                Id = y.Key,
                WinRate = CalculateWinRate(answers, y.Key)
            };
            return res;
        }).ToList();

        var modelScale = 1 / overallWinRates.Where(x => IsHuman(x.Id) == false)
            .Sum(y => y.WinRate);
        var humanScale = 1 / overallWinRates.Where(x => IsHuman(x.Id))
            .Sum(y => y.WinRate);
        var skipScale = double.IsInfinity(modelScale);
        if (skipScale)
        {
            modelScale = 1;
        }
        var skipHumanScale = double.IsInfinity(humanScale);
        if (skipHumanScale)
        {
            humanScale = 1;
        }


        var leaderBoard = new CalculateLeaderboardResponse
        {
            MostLikedModels = statsByUser.Where(x => IsHuman(x.Id) == false)
                .OrderByDescending(x => x.GetScore())
                .Select(x => new ModelTotalScore
                {
                    Id = x.Id,
                    TotalScore = x.GetScore()
                }).ToList(),
            MostLikedModelsByLlm = statsByUser.Where(x => IsHuman(x.Id) == false)
                .OrderByDescending(x => x.StartingUpVotes)
                .Select(x => new ModelTotalStartUpVotes
                {
                    Id = x.Id,
                    StartingUpVotes = x.GetScore()
                })
                .ToList(),
            
            AnswererWinRate = overallWinRates,
            HumanWinRate = overallWinRates.Where(x => IsHuman(x.Id))
                .Select(x => new LeaderBoardWinRate
                {
                    Id = x.Id,
                    WinRate = x.WinRate * (skipHumanScale ? 1 : humanScale) * 100
                }).ToList(),
            ModelWinRate = overallWinRates.Where(x => IsHuman(x.Id) == false)
                .Select(x => new ModelWinRate
                {
                    Id = x.Id,
                    WinRate = x.WinRate * (skipScale ? 1 : modelScale) * 100
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

    bool IsHuman(string id)
    {
        return id == "accepted" || id == "most-voted";
    }
    
    
    /// <summary>
    /// Take all answers, group by PostId derived user, select the answer with the highest score
    /// all divided by the total question count
    /// </summary>
    /// <param name="statTotalsList"></param>
    /// <param name="name"></param>
    /// <param name="questionCount"></param>
    /// <returns></returns>
    double CalculateWinRate(List<StatTotals> statTotalsList, string name)
    {
        var questionsIncluded = statTotalsList.Where(x => x.Id.Contains("-" + name)).Select(x => x.PostId).Distinct().ToList();
        var questionsAnswered = questionsIncluded.Count;
        if (questionsAnswered == 0)
        {
            return 0;
        }
        
        var winRate = statTotalsList.Where(x => questionsIncluded.Contains(x.PostId))
            .GroupBy(x => x.PostId)
            .Select(x => x.OrderByDescending(y => y.GetScore()).First())
            .Count(x => x.Id.Contains("-" + name)) / (double) questionsAnswered;
        
        return winRate;
    }

    public async Task<object> Any(GetLeaderboardStatsByTag request)
    {
        var statTotals = await Db.SelectAsync<StatTotals>(@"SELECT st.*
FROM main.StatTotals st
         JOIN main.post p ON st.PostId = p.Id
WHERE (p.Tags LIKE @TagMiddle OR p.Tags LIKE @TagLeft OR p.Tags LIKE @TagRight OR p.Tags = @TagSolo)", new { TagSolo = $"[{request.Tag}]", 
            TagRight = $"%,{request.Tag}", 
            TagLeft = $"{request.Tag},%",
            TagMiddle = $",{request.Tag},",
        });
        // filter to answers only
        var answers = statTotals.Where(x => x.Id.Contains('-') 
                                            && !x.Id.Contains("-accepted") 
                                            && !x.Id.Contains("-most-voted")
                                            && !x.Id.Contains("-undefined")).ToList();
        // Sum up votes by model, first group by UserName
        var statsByUser = answers.GroupBy(x => x.Id.SplitOnFirst('-')[1]).Select(x => new StatTotals
        {
            Id = x.Key,
            UpVotes = x.Sum(y => y.UpVotes),
            DownVotes = x.Sum(y => y.DownVotes),
            StartingUpVotes = x.Sum(y => y.StartingUpVotes),
            FavoriteCount = x.Sum(y => y.FavoriteCount)
        }).ToList();

        return CalculateLeaderboardResponse(statsByUser,answers);
    }

    public async Task<object> Any(GetLeaderboardStatsHuman request)
    {
        var statTotals = await Db.SelectAsync<StatTotals>(@"
select * from main.StatTotals where PostId in (select PostId from StatTotals
where PostId in (select StatTotals.PostId from StatTotals
                 where Id like '%-accepted')
group by PostId) and  (Id like '%-accepted' or Id like '%-most-voted' or Id not like '%-%')");
        // filter to answers only
        var answers = statTotals.Where(x => x.Id.Contains('-') 
                                            && !x.Id.Contains("-accepted") 
                                            && !x.Id.Contains("-most-voted")
                                            && !x.Id.Contains("-undefined")).ToList();
        // Sum up votes by model, first group by UserName
        var statsByUser = answers.GroupBy(x => x.Id.SplitOnFirst('-')[1]).Select(x => new StatTotals
        {
            Id = x.Key,
            UpVotes = x.Sum(y => y.UpVotes),
            DownVotes = x.Sum(y => y.DownVotes),
            StartingUpVotes = x.Sum(y => y.StartingUpVotes),
            FavoriteCount = x.Sum(y => y.FavoriteCount)
        }).ToList();

        return CalculateLeaderboardResponse(statsByUser,answers);
    }
}

public class GetLeaderboardStatsHuman
{
}

public class GetLeaderboardStatsByTag
{
    public string Tag { get; set; }
}

public class CalculateLeaderboardResponse
{
    public List<ModelTotalScore> MostLikedModels { get; set; }
    public List<ModelTotalStartUpVotes> MostLikedModelsByLlm { get; set; }
    public List<LeaderBoardWinRate> AnswererWinRate { get; set; }
    public List<LeaderBoardWinRate> HumanVsLlmWinRateByHumanVotes { get; set; }
    public List<LeaderBoardWinRate> HumanVsLlmWinRateByLlmVotes { get; set; }
    public List<ModelWinRateByTag> ModelWinRateByTag { get; set; }
    public List<ModelTotalScore> ModelTotalScore { get; set; }
    public List<ModelTotalScoreByTag> ModelTotalScoreByTag { get; set; }
    public List<ModelWinRate> ModelWinRate { get; set; }
    public List<LeaderBoardWinRate> HumanWinRate { get; set; }
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
}

public class LeaderBoardWinRate
{
    public string Id { get; set; }
    public double WinRate { get; set; }
}

public class LeaderBoardWinRateByTag
{
    public string Id { get; set; }
    public string Tag { get; set; }
    public double WinRate { get; set; }
}

public class CalculateLeaderBoard : IReturn<CalculateLeaderboardResponse>, IGet
{
    
}