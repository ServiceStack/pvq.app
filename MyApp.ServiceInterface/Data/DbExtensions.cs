﻿using System.Data;
using MyApp.ServiceInterface;
using MyApp.ServiceInterface.App;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Jobs;
using ServiceStack.Messaging;
using ServiceStack.OrmLite;

namespace MyApp.Data;

public static class DbExtensions
{
    public static SqlExpression<Post> WhereContainsTag(this SqlExpression<Post> q, string? tag)
    {
        if (tag != null)
        {
            tag = tag.UrlDecode().Replace("'","").Replace("\\","").SqlVerifyFragment();
            q.UnsafeWhere("',' || Tags || ',' LIKE '%," + tag + ",%'");
        }
        return q;
    }
    
    public static SqlExpression<PostFts> WhereContainsTag(this SqlExpression<PostFts> q, string? tag)
    {
        if (tag != null)
        {
            tag = tag.UrlDecode().Replace("'","").Replace("\\","");
            q.UnsafeWhere($"Tags match '\"{tag}\"'");
        }
        return q;
    }
    
    public static SqlExpression<Post> OrderByView(this SqlExpression<Post> q, string? view)
    {
        view = view?.ToLower();
        if (view is "popular" or "most-views")
            q.OrderByDescending(x => x.ViewCount);
        else if (view is null or "" or "interesting" or "most-votes")
            q.OrderByDescending(x => x.Score);
        else
            q.OrderByDescending(x => x.Id);
        return q;
    }
    
    public static SqlExpression<PostFts> OrderByView(this SqlExpression<PostFts> q, string? view)
    {
        view = view?.ToLower();
        if (view == "newest")
            q.OrderByDescending("ModifiedDate");
        else if (view == "oldest")
            q.OrderBy("ModifiedDate");
        else
            q.OrderBy("Rank");
        return q;
    }

    public static Post ToPost(this PostFts x) => new Post
    {
        Id = x.RefId.LeftPart('-').ToInt(),
        PostTypeId = x.RefId.Contains('-') ? 2 : 1,
        Summary = x.Body.GenerateSummary(),
        CreationDate = x.ModifiedDate,
        LastEditDate = x.ModifiedDate,
    };

    public static List<Post> PopulatePosts(this IDbConnection db, List<PostFts> posts) =>
        db.PopulatePosts(posts.Select(ToPost).ToList());
    
    public static List<Post> PopulatePosts(this IDbConnection db, List<Post> posts)
    {
        var postIds = posts.Select(x => x.Id).ToSet();
        var fullPosts = db.Select(db.From<Post>().Where(x => postIds.Contains(x.Id)));
        var fullPostsMap = fullPosts.ToDictionary(x => x.Id);

        foreach (var post in posts)
        {
            if (fullPostsMap.TryGetValue(post.Id, out var fullPost))
            {
                post.Title = fullPost.Title;
                post.Slug = fullPost.Slug;
                if (post.PostTypeId == 1)
                {
                    post.Tags = fullPost.Tags;
                    post.Score = fullPost.Score;
                    post.ViewCount = fullPost.ViewCount;
                    post.CreationDate = fullPost.CreationDate;
                    post.LastEditDate = fullPost.LastEditDate;
                }
            }
        }
        return posts;
    }

    public static bool IsWatchingPost(this IDbConnection db, string userName, int? postId) => 
        db.Exists(db.From<WatchPost>().Where(x => x.UserName == userName && x.PostId == postId));

    public static bool IsWatchingTag(this IDbConnection db, string userName, string tag) => 
        db.Exists(db.From<WatchTag>().Where(x => x.UserName == userName && x.Tag == tag));

    public static void NotifyQuestionAuthorIfRequired(this IDbConnection db, IBackgroundJobs jobs, Post answer)
    {
        // Only add notifications for answers older than 1hr
        var post = db.SingleById<Post>(answer.ParentId);
        if (post?.CreatedBy != null && DateTime.UtcNow - post.CreationDate > TimeSpan.FromHours(1))
        {
            if (!string.IsNullOrEmpty(answer.Summary))
            {
                jobs.RunCommand<CreateNotificationCommand>(new Notification {
                    UserName = post.CreatedBy,
                    PostId = post.Id,
                    Type = NotificationType.NewAnswer,
                    CreatedDate = DateTime.UtcNow,
                    RefId = answer.RefId!,
                    Summary = answer.Summary,
                    RefUserName = answer.CreatedBy,
                });
            }
        }
    }
}
