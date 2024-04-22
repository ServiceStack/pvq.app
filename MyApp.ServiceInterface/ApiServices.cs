using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class ApiServices(IDbConnectionFactory DbFactory) : Service
{
    public async Task<object> Any(SearchPosts request)
    {
        using var dbSearch = await DbFactory.OpenAsync(Databases.Search);
        var q = dbSearch.From<PostFts>();
            
        var search = request.Q?.Trim() ?? "";
        if (search.StartsWith('[') && search.EndsWith(']'))
        {
            q.WhereContainsTag(search.TrimStart('[').TrimEnd(']'));
        }
        else
        {
            var searchPhrase = string.Join(" AND ", search.Split(' ').Select(x => '"' + x.Trim().StripQuotes() + '"'));
            q.Where("Body match {0}", searchPhrase);
        }

        q.OrderByView(request.View);

        var take = Math.Min(request.Take ?? 25, 200);
        List<PostFts> postsFts = await dbSearch.SelectAsync(q
            .Select("RefId, substring(Body,0,400) as Body, ModifiedDate")
            .Skip(request.Skip)
            .Take(take));
        var total = dbSearch.Count(q);

        using var db = await DbFactory.OpenAsync();
        var posts = await db.PopulatePostsAsync(postsFts);

        return new SearchPostsResponse
        {
            Total = total,
            Results = posts,
        };
    }
}
