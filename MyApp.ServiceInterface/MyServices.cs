using Microsoft.AspNetCore.Http;
using ServiceStack;
using MyApp.ServiceModel;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace MyApp.Data;

public class MyServices : Service
{
    public object Any(Hello request)
    {
        return new HelloResponse { Result = $"Hello, {request.Name}!" };
    }

    public async Task<object> Any(AdminData request)
    {
        var tables = new (string Label, Type Type)[] 
        {
            ("Posts", typeof(Post)),
        };
        var dialect = Db.GetDialectProvider();
        var totalSql = tables.Map(x => $"SELECT '{x.Label}', COUNT(*) FROM {dialect.GetQuotedTableName(x.Type.GetModelMetadata())}")
            .Join(" UNION ");
        var results = await Db.DictionaryAsync<string,int>(totalSql);
        
        return new AdminDataResponse {
            PageStats = tables.Map(x => new PageStats {
                Label = x.Label, 
                Total = results[x.Label],
            })
        };
    }

    [AddHeader(ContentType = MimeTypes.PlainText)]
    public async Task<object> Any(GetRequestInfo request)
    {
        var sb = StringBuilderCache.Allocate();
        var aspReq = (HttpRequest)Request!.OriginalRequest;
        sb.AppendLine($"{aspReq.Method} {aspReq.Path}{aspReq.QueryString}");
        foreach (var header in aspReq.Headers)
        {
            if (header.Key == "Cookie") continue;
            sb.AppendLine($"{header.Key}: {header.Value}");
        }
        
        sb.AppendLine("\nCookies:");
        foreach (var cookie in aspReq.Cookies)
        {
            sb.AppendLine($"{cookie.Key}: {cookie.Value}");
        }

        sb.AppendLine("\nRemote IP: " + aspReq.HttpContext.GetRemoteIp());
        return StringBuilderCache.ReturnAndFree(sb);
    }
}
