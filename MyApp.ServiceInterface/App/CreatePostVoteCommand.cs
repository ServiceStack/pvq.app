using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceInterface.Renderers;
using MyApp.ServiceModel;
using ServiceStack.Jobs;

namespace MyApp.ServiceInterface.App;

[Worker(Databases.App)]
[Tag(Tags.Database)]
public class CreatePostVoteCommand(AppConfig appConfig, IDbConnection db, IBackgroundJobs jobs) 
    : SyncCommand<Vote>
{
    protected override void Run(Vote vote)
    {
        if (string.IsNullOrEmpty(vote.RefId))
            throw new ArgumentNullException(nameof(vote.RefId));
        if (string.IsNullOrEmpty(vote.UserName))
            throw new ArgumentNullException(nameof(vote.UserName));

        var isAnswer = vote.RefId.IndexOf('-') >= 0;
        var voteUp = isAnswer ? AchievementType.AnswerUpVote : AchievementType.QuestionUpVote; 
        var voteDown = isAnswer ? AchievementType.AnswerDownVote : AchievementType.QuestionDownVote; 
                
        var rowsDeleted = db.Delete<Vote>(new { vote.RefId, vote.UserName });
        if (rowsDeleted > 0 && vote.RefUserName != null)
        {
            // If they rescinded their previous vote, also remove the Ref User's previous achievement for that Q or A
            db.ExecuteNonQuery(
                "DELETE FROM Achievement WHERE UserName = @TargetUser AND RefUserName = @VoterUserName AND RefId = @RefId AND Type IN (@voteUp,@voteDown)",
                new { TargetUser = vote.RefUserName, VoterUserName = vote.UserName , vote.RefId, voteUp, voteDown });
        }
            
        if (vote.Score != 0)
        {
            db.Insert(vote);

            if (appConfig.IsHuman(vote.RefUserName))
            {
                db.Insert(new Achievement
                {
                    UserName = vote.RefUserName!, // User who's Q or A was voted on
                    RefUserName = vote.UserName,  // User who voted
                    PostId = vote.PostId,
                    RefId = vote.RefId,
                    Type = vote.Score > 0 ? voteUp : voteDown,
                    Score = vote.Score > 0 ? 10 : -1, // 10 points for UpVote, -1 point for DownVote
                    CreatedDate = DateTime.UtcNow,
                });
                appConfig.IncrUnreadAchievementsFor(vote.RefUserName!);
            }
        }

        jobs.RunCommand<RegenerateMetaCommand>(new RegenerateMeta { ForPost = vote.PostId });
    }
}
