﻿using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using MyApp.Data;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface.App;

[Tag(Tags.Database)]
public class DeletePostsCommand(AppConfig appConfig, IDbConnection db) : IAsyncCommand<DeletePosts>
{
    public async Task ExecuteAsync(DeletePosts request)
    {
        foreach (var postId in request.Ids)
        {
            await db.DeleteAsync<PostJob>(x => x.PostId == postId);
            await db.DeleteAsync<Vote>(x => x.PostId == postId);
            await db.DeleteByIdAsync<Post>(postId);
            await db.DeleteAsync<StatTotals>(x => x.PostId == postId);
            await db.DeleteAsync<Notification>(x => x.PostId == postId);
            await db.DeleteAsync<WatchPost>(x => x.PostId == postId);
            await db.DeleteAsync<PostEmail>(x => x.PostId == postId);
            appConfig.ResetInitialPostId(db);
        }
    }
}
