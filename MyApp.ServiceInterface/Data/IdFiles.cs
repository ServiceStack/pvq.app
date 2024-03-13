using System.Collections.Concurrent;
using ServiceStack.IO;

namespace MyApp.Data;

public class IdFiles(int id, string dir1, string dir2, string fileId, List<IVirtualFile> files)
{
    public int Id { get; init; } = id;
    public string Dir1 { get; init; } = dir1;
    public string Dir2 { get; init; } = dir2;
    public string FileId { get; init; } = fileId;
    public List<IVirtualFile> Files { get; init; } = files;
    public ConcurrentDictionary<string, string> FileContents { get; } = [];

    public async Task LoadContentsAsync()
    {
        if (FileContents.Count > 0) return;
        var tasks = new List<Task>();
        tasks.AddRange(Files.Select(async file => {
            FileContents[file.Name] = await file.ReadAllTextAsync();
        }));
        await Task.WhenAll(tasks);
    }
}