using MyApp.ServiceModel;

namespace MyApp;

public static class UiUtils
{
    public const int MaxPageSize = 50;
    public const int DefaultPageSize = 25;
    static DateTime MinDateTime = new(2008, 8, 1);

    public static int ToPageSize(this int? pageSize) => pageSize != null
        ? Math.Min(MaxPageSize, pageSize.Value)
        : DefaultPageSize;

    public static DateTime GetModifiedDate(this Post post)
    {
        var date = post.LastEditDate ?? post.CreationDate;
        return date < MinDateTime ? MinDateTime : date;
    }
}
