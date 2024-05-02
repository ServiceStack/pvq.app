using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class SearchServices(ILogger<SearchServices> log, QuestionsProvider questions) : Service
{
    public async Task Any(SearchTasks request)
    {
        using var db = HostContext.AppHost.GetDbConnection(Databases.Search);
        string QuotedValue(string? value) => db.GetDialectProvider().GetQuotedValue(value);
        var minDate = new DateTime(2008,08,1);

        if (request.AddPostToIndex != null)
        {
            var id = request.AddPostToIndex.Value;

            log.LogInformation("Adding Post '{PostId}' Question to Search Index...", id);

            var post = await questions.GetQuestionFileAsPostAsync(id);
            if (post.Id == default)
                throw new ArgumentNullException(nameof(post.Id));

            var refId = post.RefId ?? post.GetRefId();
            log.LogDebug("Adding Question {Id} {Title}", post.Id, post.Title);
            
            var modifiedDate = post.LastEditDate ?? (post.CreationDate > minDate ? post.CreationDate : minDate);
            await db.ExecuteNonQueryAsync($"DELETE FROM {nameof(PostFts)} WHERE rowid = {post.Id}");
            await db.ExecuteNonQueryAsync($@"INSERT INTO {nameof(PostFts)} (
                rowid,
                {nameof(PostFts.RefId)},
                {nameof(PostFts.UserName)},
                {nameof(PostFts.Body)},
                {nameof(PostFts.Tags)},
                {nameof(PostFts.ModifiedDate)}
            ) VALUES (
                {post.Id},
                '{refId}',
                'stackoverflow',
                {QuotedValue(post.Title + "\n\n" + post.Body)},
                {QuotedValue(string.Join(',', post.Tags))},
                {QuotedValue(modifiedDate.ToString("yyyy-MM-dd HH:mm:ss"))}
            )");
        }
        else if (request.AddAnswerToIndex != null)
        {
            var answerId = request.AddAnswerToIndex;
            var post = await questions.GetAnswerAsPostAsync(answerId);
            if (post == null)
            {
                log.LogError("Answer '{AnswerId}' does not exist", answerId);
                return;
            }
            
            var nextId = await db.ScalarAsync<int>("SELECT MAX(rowid) FROM PostFts");
            nextId += 1;

            var refId = post.RefId ?? post.GetRefId();

            log.LogInformation("Adding Answer '{RefId}' to Search Index...", answerId);

            var modifiedDate = post.LastEditDate ?? (post.CreationDate > minDate ? post.CreationDate : minDate);
            await db.ExecuteNonQueryAsync($"DELETE FROM {nameof(PostFts)} where {nameof(PostFts.RefId)} = {QuotedValue(refId)}");
            await db.ExecuteNonQueryAsync($@"INSERT INTO {nameof(PostFts)} (
                rowid,
                {nameof(PostFts.RefId)},
                {nameof(PostFts.UserName)},
                {nameof(PostFts.Body)},
                {nameof(PostFts.ModifiedDate)}
            ) VALUES (
                {nextId},
                {QuotedValue(refId)},
                {QuotedValue(post.CreatedBy)},
                {QuotedValue(post.Body)},
                {QuotedValue(modifiedDate.ToString("yyyy-MM-dd HH:mm:ss"))}
            )");
        }
        
        if (request.DeletePost != null)
        {
            var id = request.DeletePost.Value;
            log.LogInformation("Deleting Post '{PostId}' from Search Index...", id);
            await db.ExecuteNonQueryAsync($"DELETE FROM PostFts where RefId = '{id}' or RefId LIKE '{id}-%'");
        }
    }
}
