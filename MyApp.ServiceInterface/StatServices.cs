using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public enum PostGroup
{
    Top1K,
}

public class QuestionGroup
{
    public int Id { get; set; }
    public PostGroup Group { get; set; }
}

public class StatServices(AppConfig appConfig) : Service
{
    public async Task<object> Any(MissingTop100 request)
    {
        var top1kIds = await Db.ColumnAsync<int>(Db.From<QuestionGroup>()
            .Where(x => x.Group == PostGroup.Top1K));

        var model = appConfig.GetModelUser(request.Model)
            ?? throw HttpError.NotFound("Model not found");
            
        var existingQuestionIds = await Db.ColumnDistinctAsync<int>(
            $"""
            SELECT PostId from StatTotals S WHERE Id LIKE '%-{model.UserName}'
            AND EXISTS (SELECT Id FROM QuestionGroup where Id = S.PostId AND "Group" = 'Top1K');
            """);

        var missingIds = top1kIds.Where(x => !existingQuestionIds.Contains(x)).ToList();
        
        return new MissingTop100Response { Results = missingIds };
    }
}