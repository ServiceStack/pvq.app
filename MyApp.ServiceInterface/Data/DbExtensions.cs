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
            q.UnsafeWhere("',' || tags || ',' like '%," + tag + ",%'");
        }
        return q;
    }
    
    public static SqlExpression<Post> WhereSearch(this SqlExpression<Post> q, string? search)
    {
        if (!string.IsNullOrEmpty(search))
        {
            search = search.Trim();
            if (search.StartsWith('[') && search.EndsWith(']'))
            {
                q.WhereContainsTag(search.TrimStart('[').TrimEnd(']'));
            }
            else
            {
                var sb = StringBuilderCache.Allocate();
                var words = search.Split(' ');
                for (var i = 0; i < words.Length; i++)
                {
                    if (sb.Length > 0)
                        sb.Append(" AND ");
                    sb.AppendLine("(title like '%' || {" + i + "} || '%' or summary like '%' || {" + i + "} || '%' or tags like '%' || {" + i + "} || '%')");
                }

                var sql = StringBuilderCache.ReturnAndFree(sb);
                q.UnsafeWhere(sql, words.Cast<object>().ToArray());
            }
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
