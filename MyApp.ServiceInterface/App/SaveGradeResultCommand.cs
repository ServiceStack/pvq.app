using System.Data;
using ServiceStack;
using ServiceStack.Messaging;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

public class SaveGradeResultCommand(AppConfig appConfig, IDbConnection db, IMessageProducer mq, WorkerAnswerNotifier answerNotifier) 
    : IAsyncCommand<StatTotals>
{
    public async Task ExecuteAsync(StatTotals request)
    {
        var lastUpdated = request.LastUpdated ?? DateTime.UtcNow;
        appConfig.SetLastUpdated(request.Id, lastUpdated);
        var updatedRow = await db.UpdateOnlyAsync(() => new StatTotals
        {
            StartingUpVotes = request.StartingUpVotes,
            CreatedBy = request.CreatedBy,
            LastUpdated = lastUpdated,
        }, x => x.Id == request.Id);
        
        if (updatedRow == 0)
        {
            await db.InsertAsync(request);
        }

        if (request.CreatedBy != null)
        {
            answerNotifier.NotifyNewAnswer(request.PostId, request.CreatedBy);
        }
        
        mq.Publish(new RenderComponent
        {
            RegenerateMeta = new() { ForPost = request.PostId }
        });
    }
}
