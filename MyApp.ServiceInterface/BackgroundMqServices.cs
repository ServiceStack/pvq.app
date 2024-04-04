using Microsoft.Extensions.Logging;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class BackgroundMqServices(
    ILogger<BackgroundMqServices> log,
    AppConfig appConfig, 
    R2VirtualFiles r2, 
    ModelWorkerQueue modelWorkers, 
    QuestionsProvider questions) 
    : Service
{
    public async Task Any(DiskTasks request)
    {
        var saveFile = request.SaveFile;
        if (saveFile != null)
        {
            if (saveFile.Stream != null)
            {
                await r2.WriteFileAsync(saveFile.FilePath, saveFile.Stream);
            }
            else if (saveFile.Text != null)
            {
                await r2.WriteFileAsync(saveFile.FilePath, saveFile.Text);
            }
            else if (saveFile.Bytes != null)
            {
                await r2.WriteFileAsync(saveFile.FilePath, saveFile.Bytes);
            }
        }

        if (request.CdnDeleteFiles != null)
        {
            r2.DeleteFiles(request.CdnDeleteFiles);
        }
    }

    public async Task Any(DbWrites request)
    {
        var vote = request.CreatePostVote;
        if (vote != null)
        {
            if (string.IsNullOrEmpty(vote.RefId))
                throw new ArgumentNullException(nameof(vote.RefId));
            if (string.IsNullOrEmpty(vote.UserName))
                throw new ArgumentNullException(nameof(vote.UserName));

            var isAnswer = vote.RefId.IndexOf('-') >= 0;
            var voteUp = isAnswer ? AchievementType.AnswerUpVote : AchievementType.QuestionUpVote; 
            var voteDown = isAnswer ? AchievementType.AnswerDownVote : AchievementType.QuestionDownVote; 
                
            var rowsDeleted = await Db.DeleteAsync<Vote>(new { vote.RefId, vote.UserName });
            if (rowsDeleted > 0 && vote.RefUserName != null)
            {
                // If they rescinded their previous vote, also remove the Ref User's previous achievement for that Q or A
                await Db.ExecuteNonQueryAsync(
                    "DELETE FROM Achievement WHERE UserName = @TargetUser AND RefUserName = @VoterUserName AND RefId = @RefId AND Type IN (@voteUp,@voteDown)",
                    new { TargetUser = vote.RefUserName, VoterUserName = vote.UserName , vote.RefId, voteUp, voteDown });
            }
            
            if (vote.Score != 0)
            {
                await Db.InsertAsync(vote);

                if (vote.RefUserName != null)
                {
                    await Db.InsertAsync(new Achievement
                    {
                        UserName = vote.RefUserName,
                        RefUserName = vote.UserName,
                        PostId = vote.PostId,
                        RefId = vote.RefId,
                        Type = vote.Score > 0 ? voteUp : voteDown,
                        Score = vote.Score > 0 ? 10 : -1, // 10 points for UpVote, -1 point for DownVote
                        CreatedDate = DateTime.UtcNow,
                    });
                }
            }
            
            MessageProducer.Publish(new RenderComponent {
                RegenerateMeta = vote.PostId
            });
            
            request.UpdateReputations = true;
        }

        if (request.CreatePost != null)
        {
            var postId = (int)await Db.InsertAsync(request.CreatePost, selectIdentity:true);
            var createdBy = request.CreatePost.CreatedBy;
            if (createdBy != null && request.CreatePost.PostTypeId == 1)
            {
                await appConfig.ResetUserQuestionsAsync(Db, createdBy);
            }

            try
            {
                await Db.InsertAsync(new StatTotals
                {
                    Id = $"{postId}",
                    PostId = postId,
                    UpVotes = 0,
                    DownVotes = 0,
                    StartingUpVotes = 0,
                    CreatedBy = request.CreatePost.CreatedBy,
                });
            }
            catch (Exception e)
            {
                log.LogWarning("Couldn't insert StatTotals for Post {PostId}: '{Message}', updating instead...", postId, e.Message);
                await Db.UpdateOnlyAsync(() => new StatTotals
                {
                    PostId = postId,
                    CreatedBy = request.CreatePost.CreatedBy,
                }, x => x.Id == $"{postId}");
            }
        }

        if (request.UpdatePost != null)
        {
            var question = request.UpdatePost;
            await Db.UpdateOnlyAsync(() => new Post {
                Title = question.Title,
                Tags = question.Tags,
                Slug = question.Slug,
                Summary = question.Summary,
                ModifiedBy = question.ModifiedBy,
                LastActivityDate = question.LastActivityDate,
                LastEditDate = question.LastEditDate,
            }, x => x.Id == request.UpdatePost.Id);
        }

        if (request.DeletePost != null)
        {
            await Db.DeleteAsync<PostJob>(x => x.PostId == request.DeletePost);
            await Db.DeleteAsync<Vote>(x => x.PostId == request.DeletePost);
            await Db.DeleteByIdAsync<Post>(request.DeletePost);
            AppConfig.Instance.ResetInitialPostId(Db);
        }
        
        if (request.CreatePostJobs is { Count: > 0 })
        {
            await Db.SaveAllAsync(request.CreatePostJobs);
            request.CreatePostJobs.ForEach(modelWorkers.Enqueue);
        }

        var startJob = request.StartJob;
        if (startJob != null)
        {
            await Db.UpdateOnlyAsync(() => new PostJob
            {
                StartedDate = DateTime.UtcNow,
                Worker = startJob.Worker,
                WorkerIp = startJob.WorkerIp,
            }, x => x.PostId == startJob.Id);
        }

        if (request.CompleteJobIds is { Count: > 0 })
        {
            await Db.UpdateOnlyAsync(() => new PostJob {
                    CompletedDate = DateTime.UtcNow,
                }, 
                x => request.CompleteJobIds.Contains(x.Id));
            var postJobs = await Db.SelectAsync(Db.From<PostJob>()
                .Where(x => request.CompleteJobIds.Contains(x.Id)));

            foreach (var postJob in postJobs)
            {
                // If there's no outstanding model answer jobs for this post, add a rank job
                if (!Db.Exists(Db.From<PostJob>()
                    .Where(x => x.PostId == postJob.PostId && x.CompletedDate == null)))
                {
                    var rankJob = new PostJob
                    {
                        PostId = postJob.PostId,
                        Model = "rank",
                        Title = postJob.Title,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = nameof(DbWrites),
                    };
                    await Db.InsertAsync(rankJob);
                    modelWorkers.Enqueue(rankJob);
                    MessageProducer.Publish(new SearchTasks { AddPostToIndex = postJob.PostId });
                }
            }
        }

        if (request.FailJob != null)
        {
            await Db.UpdateAddAsync(() => new PostJob {
                    Error = request.FailJob.Error,
                    RetryCount = 1,
                }, 
                x => x.PostId == request.FailJob.Id);
            var postJob = await Db.SingleByIdAsync<PostJob>(request.FailJob.Id);
            if (postJob != null)
            {
                if (postJob.RetryCount > 3)
                {
                    await Db.UpdateOnlyAsync(() =>
                            new PostJob { CompletedDate = DateTime.UtcNow },
                        x => x.PostId == request.FailJob.Id);
                }
                else
                {
                    modelWorkers.Enqueue(postJob);
                }
            }
        }

        var answer = request.CreateAnswer; 
        if (answer is { RefId: not null, ParentId: not null })
        {
            var postId = answer.ParentId.Value;
            if (!await Db.ExistsAsync(Db.From<StatTotals>().Where(x => x.Id == answer.RefId)))
            {
                await Db.InsertAsync(new StatTotals
                {
                    Id = answer.RefId,
                    PostId = postId,
                    ViewCount = 0,
                    FavoriteCount = 0,
                    UpVotes = 0,
                    DownVotes = 0,
                    StartingUpVotes = 0,
                    CreatedBy = answer.CreatedBy,
                });
            }

            var post = await Db.SingleByIdAsync<Post>(postId);
            if (post?.CreatedBy != null)
            {
                await Db.InsertAsync(new Notification
                {
                    UserName = post.CreatedBy, 
                    Type = NotificationType.NewAnswer,
                    RefId = answer.RefId,
                    PostId = postId,
                    CreatedDate = answer.CreationDate,
                    PostTitle = post.Title.SubstringWithEllipsis(0,100),
                    Summary = answer.Summary.SubstringWithEllipsis(0,100),
                    Href = $"/questions/{postId}/{post.Slug}#{answer.RefId}",
                });
            }
        }

        if (request.AnswerAddedToPost != null)
        {
            await Db.UpdateAddAsync(() => new Post
            {
                AnswerCount = 1,
            }, x => x.Id == request.AnswerAddedToPost.Value);
        }

        if (request.NewComment != null)
        {
            var refId = request.NewComment.RefId;
            var postId = refId.LeftPart('-').ToInt();
            var post = await Db.SingleByIdAsync<Post>(postId);
            if (post != null)
            {
                var isAnswer = refId.IndexOf('-') > 0;
                var createdBy = isAnswer
                    ? (await Db.SingleByIdAsync<StatTotals>(refId))?.CreatedBy
                    : post.CreatedBy;
                if (createdBy != null)
                {
                    var comment = request.NewComment.Comment;
                    await Db.InsertAsync(new Notification
                    {
                        UserName = createdBy, 
                        Type = NotificationType.NewComment,
                        RefId = refId,
                        PostId = postId,
                        CreatedDate = DateTimeOffset.FromUnixTimeMilliseconds(comment.Created).DateTime,
                        PostTitle = post.Title.SubstringWithEllipsis(0,100),
                        Summary = comment.Body.SubstringWithEllipsis(0,100),
                        Href = $"/questions/{postId}/{post.Slug}#{refId}-{comment.Created}",
                    });
                }
            }
        }

        if (request.UpdateReputations == true)
        {
            // TODO improve
            appConfig.UpdateUsersReputation(Db);
            appConfig.ResetUsersReputation(Db);
        }
    }

    public async Task Any(AnalyticsTasks request)
    {
        if (request.RecordPostView == null && request.RecordSearchView == null && request.DeletePost == null)
            return;

        using var analyticsDb = HostContext.AppHost.GetDbConnection(Databases.Analytics);
        
        if (request.RecordPostView != null)// && !Stats.IsAdminOrModerator(request.RecordPostView.UserName))
        {
            await analyticsDb.InsertAsync(request.RecordPostView);
        }

        if (request.RecordSearchView != null)// && !Stats.IsAdminOrModerator(request.RecordSearchView.UserName))
        {
            await analyticsDb.InsertAsync(request.RecordSearchView);
        }

        if (request.DeletePost != null)
        {
            await analyticsDb.DeleteAsync<PostView>(x => x.PostId == request.DeletePost);
        }
    }
}
