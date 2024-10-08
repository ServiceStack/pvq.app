﻿using System.Data;
using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Database)]
[Worker(Databases.App)]
public class CreateCommentVoteCommand(ILogger<CreateCommentVoteCommand> logger, IBackgroundJobs jobs, 
    IDbConnection db, QuestionsProvider questions) : AsyncCommand<Vote>
{
    protected override async Task RunAsync(Vote vote, CancellationToken token)
    {
        if (string.IsNullOrEmpty(vote.RefId))
            throw new ArgumentNullException(nameof(vote.RefId));
        if (string.IsNullOrEmpty(vote.UserName))
            throw new ArgumentNullException(nameof(vote.UserName));

        var log = Request.CreateJobLogger(jobs, logger);
        var rowsDeleted = db.Delete<Vote>(new { vote.RefId, vote.UserName });

        var meta = await questions.GetMetaAsync(vote.PostId);
        var created = vote.RefId.LastRightPart('-').ToLong();
        var commentTargetRefId = vote.RefId.LastLeftPart('-');
        if (meta.Comments.TryGetValue(commentTargetRefId, out var comments))
        {
            var comment = comments.Find(x => x.Created == created);
            if (comment != null)
            {
                if (comment.CreatedBy == vote.UserName)
                    throw new ArgumentException("Can't vote on your own comment", nameof(vote.RefId));

                log.LogInformation("Recording {User} Vote {Score} for comment {RefId}", comment.CreatedBy, vote.Score, vote.RefId);
                vote.RefUserName = comment.CreatedBy;
                db.Insert(vote);

                comment.UpVotes = (int) db.Count<Vote>(x => x.RefId == vote.RefId && x.Score > 0);
                await questions.SaveMetaAsync(vote.PostId, meta);
            }
        }
    }
}
