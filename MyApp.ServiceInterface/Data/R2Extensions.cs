namespace MyApp.Data;

public static class R2Extensions
{
    public static (string dir1, string dir2, string fileId) ToFileParts(this int id)
    {
        var idStr = $"{id}".PadLeft(9, '0');
        var dir1 = idStr[..3];
        var dir2 = idStr.Substring(3, 3);
        var fileId = idStr[6..];
        return (dir1, dir2, fileId);
    }
}
