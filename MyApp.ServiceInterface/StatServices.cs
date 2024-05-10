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

public class StatServices(AppConfig appConfig, QuestionsProvider questions) : Service
{
    public async Task<object> Any(MissingTop1K request)
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
        
        return new MissingTop1KResponse { Results = missingIds };
    }

    public async Task<object> Any(MissingGradedAnswersTop1K request)
    {
        var top1kIds = await Db.ColumnAsync<int>(Db.From<QuestionGroup>()
            .Where(x => x.Group == PostGroup.Top1K));

        var to = new MissingGradedAnswersTop1KResponse();
        foreach (var postId in top1kIds)
        {
            var id = $"{postId}";
            try
            {
                var meta = await questions.GetMetaAsync(postId);
                foreach (var entry in meta.ModelVotes.Safe())
                {
                    if (meta.GradedBy == null || !meta.GradedBy.ContainsKey(entry.Key))
                    {
                        var answerId = $"{postId}-{entry.Key}";
                        to.Results.Add(answerId);
                    }
                }
            }
            catch (Exception e)
            {
                to.Errors[id] = e.Message;
            }
        }

        return to;
    }
}