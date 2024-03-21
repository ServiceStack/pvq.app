using Microsoft.AspNetCore.Http;
using MyApp.ServiceModel;

namespace MyApp.Data;

public static class StatUtils
{
    public static T WithRequest<T>(this T stat, HttpContext? ctx) where T : StatBase
    {
        var user = ctx?.User;
        stat.UserName = user?.Identity?.Name;
        stat.RemoteIp = ctx?.Connection.RemoteIpAddress?.ToString();
        stat.CreatedDate = DateTime.UtcNow;
        return stat;
    }
}
