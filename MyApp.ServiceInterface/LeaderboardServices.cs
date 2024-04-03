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
        var answers = statTotals.Where(x => x.Id.Contains('-')).ToList();
        // Sum up votes by model, first group by UserName
        var statsByUser = answers.GroupBy(x => x.Id.SplitOnFirst('-')[1]).Select(x => new StatTotals
        {
            Id = x.Key,
            UpVotes = x.Sum(y => y.UpVotes),
            DownVotes = x.Sum(y => y.DownVotes),
            StartingUpVotes = x.Sum(y => y.StartingUpVotes),
            FavoriteCount = x.Sum(y => y.FavoriteCount)
        }).ToList();
        
        var leaderBoard = CalculateLeaderboardResponse(statTotals, statsByUser, answers);

        return leaderBoard;
    }

    private CalculateLeaderboardResponse CalculateLeaderboardResponse(List<StatTotals> statTotals, List<StatTotals> statsByUser, List<StatTotals> answers)
    {
        var statQuestions = statTotals.Where(x => !x.Id.Contains('-')).ToList();

        var overallWinRates = statsByUser.GroupBy(x => x.Id).Select(y =>
        {
            var res = new LeaderBoardWinRate
            {
                Id = y.Key,
                WinRate = CalculateWinRate(answers, y.Key, statQuestions.Count)
            };
            return res;
        }).ToList();

        var modelScale = 1 / overallWinRates.Where(x => IsHuman(x.Id) == false)
            .Sum(y => y.WinRate);


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
            ModelWinRate = overallWinRates.Where(x => IsHuman(x.Id) == false)
                .Select(x => new ModelWinRate
                {
                    Id = x.Id,
                    WinRate = x.WinRate * modelScale * 100
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
    double CalculateWinRate(List<StatTotals> statTotalsList, string name, int questionCount)
    {
        return ((double)statTotalsList
                    .GroupBy(a => a.PostId
                    ).Select(aq => aq
                        .MaxBy(b => b.GetScore()))
                    .Count(winner => 
                        winner != null && winner.Id.Contains("-") && winner.Id.SplitOnFirst('-')[1] == name) 
                / questionCount) * 100;
    }

    public async Task<object> Any(GetLeaderboardStatsByTag request)
    {
        var statTotals = Db.Select<StatTotals>(@"SELECT st.*
FROM main.StatTotals st
         JOIN main.post p ON st.PostId = p.Id
WHERE (p.Tags LIKE @TagMiddle OR p.Tags LIKE @TagLeft OR p.Tags LIKE @TagRight OR p.Tags = @TagSolo)", new { TagSolo = $"[{request.Tag}]", 
            TagRight = $"%,{request.Tag}", 
            TagLeft = $"{request.Tag},%",
            TagMiddle = $",{request.Tag},",
        });
        // filter to answers only
        var answers = statTotals.Where(x => x.Id.Contains('-')).ToList();
        // Sum up votes by model, first group by UserName
        var statsByUser = answers.GroupBy(x => x.Id.SplitOnFirst('-')[1]).Select(x => new StatTotals
        {
            Id = x.Key,
            UpVotes = x.Sum(y => y.UpVotes),
            DownVotes = x.Sum(y => y.DownVotes),
            StartingUpVotes = x.Sum(y => y.StartingUpVotes),
            FavoriteCount = x.Sum(y => y.FavoriteCount)
        }).ToList();

        return CalculateLeaderboardResponse(statTotals,statsByUser,answers);
    }
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