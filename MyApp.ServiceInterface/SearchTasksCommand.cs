﻿using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

[Tag(Tag.Tasks)]
[Worker(Databases.Search)]
public class SearchTasks
{
    public int? AddPostToIndex { get; set; }
    public string? AddAnswerToIndex { get; set; }
    public List<int>? DeletePosts { get; set; }
    public List<string>? DeleteAnswers { get; set; }
}

public class SearchTasksCommand(ILogger<SearchTasksCommand> log, IDbConnectionFactory dbFactory, QuestionsProvider questions) : AsyncCommand<SearchTasks>
{
    protected override async Task RunAsync(SearchTasks request, CancellationToken token)
    {
        using var db = dbFactory.Open(Databases.Search);
        string QuotedValue(string? value) => db.GetDialectProvider().GetQuotedValue(value);
        var minDate = new DateTime(2008,08,1);

        if (request.AddPostToIndex != null)
        {
            var id = request.AddPostToIndex.Value;
            var post = await questions.GetQuestionFileAsPostAsync(id);
            if (post?.Id == default)
            {
                log.LogError("[SEARCH] Question '{Id}' does not exist", id);
                return;
            }

            var refId = post.RefId ?? post.GetRefId();
            log.LogInformation("[SEARCH] Adding Question {PostId} '{Title}' to Search Index...", id, post.Title);

            var modifiedDate = post.LastEditDate ?? (post.CreationDate > minDate ? post.CreationDate : minDate);
            db.ExecuteNonQuery($"DELETE FROM {nameof(PostFts)} WHERE rowid = {post.Id}");
            db.ExecuteNonQuery($@"INSERT INTO {nameof(PostFts)} (
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
                log.LogError("[SEARCH] Answer '{AnswerId}' does not exist", answerId);
                return;
            }
            
            var refId = post.RefId ?? post.GetRefId();
            log.LogInformation("[SEARCH] Adding Answer '{RefId}' to Search Index...", answerId);

            var modifiedDate = post.LastEditDate ?? (post.CreationDate > minDate ? post.CreationDate : minDate);
            db.ExecuteNonQuery($"DELETE FROM {nameof(PostFts)} where {nameof(PostFts.RefId)} = {QuotedValue(refId)}");
            
            var nextId = db.Scalar<int>("SELECT MAX(rowid) FROM PostFts");
            nextId += 1;
            db.ExecuteNonQuery($@"INSERT INTO {nameof(PostFts)} (
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
        
        if (request.DeletePosts != null)
        {
            foreach (var id in request.DeletePosts)
            {
                log.LogInformation("[SEARCH] Deleting Post '{PostId}' from Search Index...", id);
                db.ExecuteNonQuery($"DELETE FROM PostFts where RefId = '{id}' or RefId LIKE '{id}-%'");
            }
        }
        
        if (request.DeleteAnswers != null)
        {
            foreach (var refId in request.DeleteAnswers)
            {
                log.LogInformation("[SEARCH] Deleting Answer '{PostId}' from Search Index...", refId);
                db.ExecuteNonQuery($"DELETE FROM PostFts where RefId = @refId or RefId = @refId", new { refId });
            }
        }
    }
}
