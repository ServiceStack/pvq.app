using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class ApiServices(IDbConnectionFactory dbFactory) : Service
{
    public object Any(SearchPosts request)
    {
        using var dbSearch = dbFactory.Open(Databases.Search);

        var skip = request.Skip;
        var take = Math.Min(request.Take ?? 25, 200);

        var search = request.Q?.Trim() ?? "";
        if (!string.IsNullOrEmpty(search))
        {
            var q = dbSearch.From<PostFts>();
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

            List<PostFts> postsFts = dbSearch.Select(q
                .Select("RefId, substring(Body,0,400) as Body, ModifiedDate")
                .Skip(request.Skip)
                .Take(take));
            var total = dbSearch.Count(q);

            using var db = dbFactory.Open();
            var posts = db.PopulatePosts(postsFts);

            return new SearchPostsResponse
            {
                Total = total,
                Results = posts,
            };
        }
        else
        {
            using var db = dbFactory.Open();
            var q = db.From<Post>();
            
            var posts = db.Select(q
                .OrderByView(request.View)
                .Skip(skip)
                .Take(take));

            var total = db.Count(q);

            return new SearchPostsResponse
            {
                Total = total,
                Results = posts,
            };
        }
    }
}
