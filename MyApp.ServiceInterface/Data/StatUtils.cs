using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MyApp.ServiceModel;
using ServiceStack;

namespace MyApp.Data;

public static class StatUtils
{
    public static string? GetUserName(this HttpContext? ctx) => ctx?.User.GetUserName();
    public static string? GetUserName(this ClaimsPrincipal? user) => user?.Identity?.Name;
    public static bool IsAdminOrModerator(this ClaimsPrincipal? user) => Stats.IsAdminOrModerator(user?.Identity?.Name);
    
    public static string? GetRemoteIp(this HttpContext? ctx)
    {
        var headers = ctx?.Request.Headers;
        if (headers == null)
            return null;
        return !string.IsNullOrEmpty(headers[HttpHeaders.XForwardedFor])
            ? headers[HttpHeaders.XForwardedFor].ToString()
            : !string.IsNullOrEmpty(headers[HttpHeaders.XRealIp])
                ? headers[HttpHeaders.XForwardedFor].ToString()
                : ctx?.Connection.RemoteIpAddress?.ToString();
    }

    public static T WithRequest<T>(this T stat, HttpContext? ctx) where T : StatBase
    {
        var user = ctx?.User;
        stat.UserName = user?.Identity?.Name;
        stat.RemoteIp = ctx.GetRemoteIp();
        stat.CreatedDate = DateTime.UtcNow;
        return stat;
    }
}
