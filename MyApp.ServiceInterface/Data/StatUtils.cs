using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MyApp.ServiceModel;
using ServiceStack;

namespace MyApp.Data;

public static class StatUtils
{
    public static string? GetUserName(this HttpContext? ctx) => ctx?.User.GetUserName();

    public static bool IsAdminOrModerator(this ClaimsPrincipal? user) => 
        user?.GetUserName() is "admin" || user.HasRole(Roles.Moderator); 
    
    public static string GetRequiredUserName(this ClaimsPrincipal? user) => user?.GetUserName()
        ?? throw new ArgumentNullException(nameof(user));

    public static T WithRequest<T>(this T stat, HttpContext? ctx) where T : StatBase
    {
        var user = ctx?.User;
        stat.UserName = user?.Identity?.Name;
        stat.RemoteIp = ctx.GetRemoteIp();
        stat.CreatedDate = DateTime.UtcNow;
        return stat;
    }
}
