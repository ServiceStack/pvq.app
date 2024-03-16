using ServiceStack;

namespace MyApp.Data;

public static class Urls
{
    public static string GetAvatarUrl(this string? userName) => userName != null
        ? $"/avatar/{userName}"
        : Svg.ToDataUri(Svg.GetImage(Svg.Icons.Users));
}
