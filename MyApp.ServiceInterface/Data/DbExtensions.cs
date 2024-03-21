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
        else if (view is "interesting" or "most-votes")
            q.OrderByDescending(x => x.Score);
        else
            q.OrderByDescending(x => x.Id);
        return q;
    }
}
