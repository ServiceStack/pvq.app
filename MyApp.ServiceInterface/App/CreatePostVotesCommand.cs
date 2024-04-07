using System.Data;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack.Messaging;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class CreatePostVotesCommand(AppConfig appConfig, IDbConnection db, IMessageProducer mqClient) : IAsyncCommand<Vote>
{
    public async Task ExecuteAsync(Vote vote)
    {
        if (string.IsNullOrEmpty(vote.RefId))
            throw new ArgumentNullException(nameof(vote.RefId));
        if (string.IsNullOrEmpty(vote.UserName))
            throw new ArgumentNullException(nameof(vote.UserName));

        var isAnswer = vote.RefId.IndexOf('-') >= 0;
        var voteUp = isAnswer ? AchievementType.AnswerUpVote : AchievementType.QuestionUpVote; 
        var voteDown = isAnswer ? AchievementType.AnswerDownVote : AchievementType.QuestionDownVote; 
                
        var rowsDeleted = await db.DeleteAsync<Vote>(new { vote.RefId, vote.UserName });
        if (rowsDeleted > 0 && vote.RefUserName != null)
        {
            // If they rescinded their previous vote, also remove the Ref User's previous achievement for that Q or A
            await db.ExecuteNonQueryAsync(
                "DELETE FROM Achievement WHERE UserName = @TargetUser AND RefUserName = @VoterUserName AND RefId = @RefId AND Type IN (@voteUp,@voteDown)",
                new { TargetUser = vote.RefUserName, VoterUserName = vote.UserName , vote.RefId, voteUp, voteDown });
        }
            
        if (vote.Score != 0)
        {
            await db.InsertAsync(vote);

            if (appConfig.IsHuman(vote.RefUserName))
            {
                await db.InsertAsync(new Achievement
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
        
        mqClient.Publish(new RenderComponent {
            RegenerateMeta = new() { ForPost = vote.PostId },
        });
    }
}
