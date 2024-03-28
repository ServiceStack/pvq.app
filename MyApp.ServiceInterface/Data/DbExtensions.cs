using System.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;
using ServiceStack.Text;

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
            tag = tag.UrlDecode().Replace("'","").Replace("\\","").SqlVerifyFragment();
            q.UnsafeWhere($"Tags match '\"{tag}\"'");
        }
        return q;
    }
    
    public static SqlExpression<Post> WhereSearch(this SqlExpression<Post> q, string? search, int? skip, int take)
    {
        if (!string.IsNullOrEmpty(search))
        {
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
        Summary = x.Body.StripHtml().SubstringWithEllipsis(0, 200),
        CreationDate = x.ModifiedDate,
        LastEditDate = x.ModifiedDate,
    };

    public static async Task<List<Post>> PopulatePostsAsync(this IDbConnection db, List<PostFts> posts) =>
        await db.PopulatePostsAsync(posts.Select(ToPost).ToList());
    
    public static async Task<List<Post>> PopulatePostsAsync(this IDbConnection db, List<Post> posts)
    {
        var postIds = posts.Select(x => x.Id).ToSet();
        var fullPosts = await db.SelectAsync(db.From<Post>().Where(x => postIds.Contains(x.Id)));
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
}
