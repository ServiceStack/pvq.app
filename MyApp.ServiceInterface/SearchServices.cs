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
        if (request.AddPostId != null)
        {
            var id = request.AddPostId.Value;
            var questionFiles = await questions.GetQuestionAsync(id);
            
            log.LogInformation("Adding Post '{PostId}' Question and {AnswerCount} to Search Index...", 
                id,
                questionFiles.GetAnswerFiles().Count());
            using var db = HostContext.AppHost.GetDbConnection(Databases.Search);
            var nextId = await db.ScalarAsync<int>("SELECT MAX(rowid) FROM PostFts");
            nextId += 1;
            var existingIds = new HashSet<int>();
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
                        if (!existingIds.Add(post.Id)) continue;
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
                            '{post.Id}',
                            'stackoverflow',
                            {QuotedValue(post.Title + "\n\n" + post.Body)},
                            {QuotedValue(string.Join(',',post.Tags))},
                            {QuotedValue(modifiedDate.ToString("yyyy-MM-dd HH:mm:ss"))}
                        )");
                    }
                    else if (fileType.StartsWith("h."))
                    {
                        var post = await ToPostAsync(file);
                        if (!existingIds.Add(post.Id)) continue;
                        var userName = fileType.Substring(2); 
                        log.LogDebug("Adding Human Answer {FilePath}", file.VirtualPath);
                        var modifiedDate = post.LastEditDate ?? (post.CreationDate > minDate ? post.CreationDate : minDate);
                        db.ExecuteNonQuery($@"INSERT INTO {nameof(PostFts)} (
                            rowid,
                            {nameof(PostFts.RefId)},
                            {nameof(PostFts.UserName)},
                            {nameof(PostFts.Body)},
                            {nameof(PostFts.ModifiedDate)}
                        ) VALUES (
                            {post.Id},
                            '{id}-{userName}',
                            '{userName}',
                            {QuotedValue(post.Body)},
                            {QuotedValue(modifiedDate.ToString("yyyy-MM-dd HH:mm:ss"))}
                        )");
                    }
                    else if (fileType.StartsWith("a."))
                    {
                        var json = await file.ReadAllTextAsync();
                        var obj = (Dictionary<string,object>)JSON.parse(json);
                        var choices = (List<object>) obj["choices"];
                        var choice = (Dictionary<string,object>)choices[0];
                        var message = (Dictionary<string,object>)choice["message"];
                        var body = (string)message["content"];
                        var userName = fileType.Substring(2); 
                        var modifiedDate = obj.TryGetValue("created", out var oCreated) && oCreated is int created
                            ? DateTimeOffset.FromUnixTimeSeconds(created).DateTime
                            : file.LastModified;
                        log.LogDebug("Adding Model Answer {FilePath} {UserName}", file.VirtualPath, userName);
                        db.ExecuteNonQuery($@"INSERT INTO {nameof(PostFts)} (
                            rowid,
                            {nameof(PostFts.RefId)},
                            {nameof(PostFts.UserName)},
                            {nameof(PostFts.Body)},
                            {nameof(PostFts.ModifiedDate)}
                        ) VALUES (
                            {nextId++},
                            '{id}-{userName}',
                            '{userName}',
                            {QuotedValue(body)},
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
    }
    
    async Task<Post> ToPostAsync(IVirtualFile file)
    {
        var json = await file.ReadAllTextAsync();
        var post = json.FromJson<Post>();
        return post;
    }

}