﻿using System.Data;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack.Messaging;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface.App;

public class SaveGradeResultCommand(IDbConnection db, IMessageProducer mq, WorkerAnswerNotifier answerNotifier) : IAsyncCommand<StatTotals>
{
    public async Task ExecuteAsync(StatTotals request)
    {
        var updatedRow = await db.UpdateOnlyAsync(() => new StatTotals
        {
            StartingUpVotes = request.StartingUpVotes,
            CreatedBy = request.CreatedBy,
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