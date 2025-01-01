using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class LeaderboardServices(IBackgroundJobs jobs) : Service
{
    public object Any(CalculateLeaderBoard request)
    {
        var jobRef = jobs.EnqueueCommand<LeaderboardCommand>(request);
        return jobRef;
    }

    public object Any(CalculateTop1KLeaderboard request)
    {
        var jobRef = jobs.EnqueueCommand<LeaderboardTop1KCommand>(request);
        return jobRef;
    }

    public async Task<object> Any(GetLeaderboardStatsByTag request)
    {
        var allStatsForTag = Db.Select<StatTotals>(@"SELECT st.*
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
        var modelsToExclude = request.ModelsToExclude?.Split(",").ToList() ?? [];
        // filter to answers only
        var answers = allStatsForTag.Where(x => LeaderboardUtils.FilterSpecificModels(x,modelsToExclude)).ToList();
        // Sum up votes by model, first group by UserName
        var statsByUser = answers.GroupBy(x => x.Id.RightPart('-')).Select(x => new StatTotals
        {
            Id = x.Key,
            UpVotes = x.Sum(y => y.UpVotes),
            DownVotes = x.Sum(y => y.DownVotes),
            StartingUpVotes = x.Sum(y => y.StartingUpVotes),
            FavoriteCount = x.Sum(y => y.FavoriteCount)
        }).ToList();

        var result = LeaderboardUtils.CalculateLeaderboardResponse(statsByUser,answers);

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

public class LeaderboardInfo
{
    public long AnswerCount { get; set; }
    public DateTime GeneratedDate { get; set; }
}

public class CalculateLeaderBoard : IReturn<BackgroundJobRef>, IGet
{
    public string? ModelsToExclude { get; set; }
}

public class CalculateTop1KLeaderboard : IReturn<BackgroundJobRef>, IGet
{
    public string? ModelsToExclude { get; set; }
}