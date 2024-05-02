using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class SearchServices(ILogger<SearchServices> log, QuestionsProvider questions) : Service
{
    public async Task Any(SearchTasks request)
    {
        if (request.AddPostToIndex != null)
        {
            var id = request.AddPostToIndex.Value;
            var questionFiles = await questions.GetQuestionAsync(id);
            
            log.LogInformation("Adding Post '{PostId}' Question and {AnswerCount} to Search Index...", 
                id,
                questionFiles.GetAnswerFiles().Count());
            using var db = HostContext.AppHost.GetDbConnection(Databases.Search);
            var nextId = await db.ScalarAsync<int>("SELECT MAX(rowid) FROM PostFts");
            nextId += 1;
            var existingIds = new HashSet<string>();
            var minDate = new DateTime(2008,08,1);

            string QuotedValue(string? value) => db.GetDialectProvider().GetQuotedValue(value);

            var i = 0;
            foreach (var file in questionFiles.Files)
            {
                try
                {
                    var fileType = file.Name.RightPart('.').LastLeftPart('.');
                    if (fileType == "json")
                    {
                        var post = await ToPostAsync(file);
                        if (post.Id == default)
                            throw new ArgumentNullException(nameof(post.Id));
                        var refId = post.RefId ?? $"{post.Id}";
                        if (!existingIds.Add(refId)) 
                            continue;
                        log.LogDebug("Adding Question {FilePath}", file.VirtualPath);
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
                            {QuotedValue(string.Join(',',post.Tags))},
                            {QuotedValue(modifiedDate.ToString("yyyy-MM-dd HH:mm:ss"))}
                        )");
                    }
                    else if (fileType.StartsWith("h."))
                    {
                        var post = await ToPostAsync(file);
                        post.CreatedBy ??= fileType.Substring(2); 
                        var refId = post.RefId ?? post.GetRefId();
                        if (!existingIds.Add(refId))
                            continue;

                        log.LogDebug("Adding Human Answer {FilePath}", file.VirtualPath);
                        var modifiedDate = post.LastEditDate ?? (post.CreationDate > minDate ? post.CreationDate : minDate);
                        await db.ExecuteNonQueryAsync($"DELETE FROM {nameof(PostFts)} where {nameof(PostFts.RefId)} = {QuotedValue(refId)}");
                        await db.ExecuteNonQueryAsync($@"INSERT INTO {nameof(PostFts)} (
                            rowid,
                            {nameof(PostFts.RefId)},
                            {nameof(PostFts.UserName)},
                            {nameof(PostFts.Body)},
                            {nameof(PostFts.ModifiedDate)}
                        ) VALUES (
                            {nextId++},
                            {QuotedValue(refId)},
                            {QuotedValue(post.CreatedBy)},
                            {QuotedValue(post.Body)},
                            {QuotedValue(modifiedDate.ToString("yyyy-MM-dd HH:mm:ss"))}
                        )");
                    }
                    else
                    {
                        log.LogDebug("Skipping {FilePath}", file.VirtualPath);
                    }
                }
                catch (Exception e)
                {
                    log.LogError(e, "Error indexing {File}", file.VirtualPath);
                }
            }
        }
        
        if (request.DeletePost != null)
        {
            var id = request.DeletePost.Value;
            log.LogInformation("Deleting Post '{PostId}' from Search Index...", id);
            using var db = HostContext.AppHost.GetDbConnection(Databases.Search);
            await db.ExecuteNonQueryAsync($"DELETE FROM PostFts where RefId = '{id}' or RefId LIKE '{id}-%'");
        }
    }
    
    async Task<Post> ToPostAsync(IVirtualFile file)
    {
        var json = await file.ReadAllTextAsync();
        var post = json.FromJson<Post>();
        return post;
    }
}
