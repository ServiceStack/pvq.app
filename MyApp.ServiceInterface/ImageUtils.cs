using MyApp.Data;
using ServiceStack;
using ServiceStack.Host;
using ServiceStack.Web;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace MyApp.ServiceInterface;

public static class ImageUtils
{
    public const int MaxAvatarSize = 1024 * 1024;

    public static async Task<MemoryStream> CropAndResizeAsync(Stream inStream, int width, int height, IImageFormat format)
    {
        var outStream = new MemoryStream();
        using (var image = await Image.LoadAsync(inStream))
        {
            var clone = image.Clone(context => context
                .Resize(new ResizeOptions {
                    Mode = ResizeMode.Crop,
                    Size = new Size(width, height),
                }));
            await clone.SaveAsync(outStream, format);
        }
        outStream.Position = 0;
        return outStream;
    }

    public static async Task<IHttpFile?> TransformAvatarAsync(FilesUploadContext ctx)
    {
        var originalMs = await ctx.File.InputStream.CopyToNewMemoryStreamAsync();

        var resizedMs = await CropAndResizeAsync(originalMs, 128, 128, PngFormat.Instance);

        // Offload persistence of original image to background task
        originalMs.Position = 0;
        using var mqClient = HostContext.AppHost.GetMessageProducer(ctx.Request);
        mqClient.Publish(new DiskTasks
        {
            SaveFile = new()
            {
                FilePath = ctx.Location.ResolvePath(ctx),
                Stream = originalMs,
            }
        });

        return new HttpFile(ctx.File)
        {
            FileName = $"{ctx.FileName.LastLeftPart('.')}_128.{ctx.File.FileName.LastRightPart('.')}",
            ContentLength = resizedMs.Length,
            InputStream = resizedMs,
        };
    }
}
