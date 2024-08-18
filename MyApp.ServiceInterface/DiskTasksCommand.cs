using MyApp.Data;
using ServiceStack;
using ServiceStack.IO;

namespace MyApp.ServiceInterface;

public class DiskTasksCommand(R2VirtualFiles r2, QuestionsProvider questions) : AsyncCommand<DiskTasks>
{
    protected override async Task RunAsync(DiskTasks request, CancellationToken token)
    {
        var saveFile = request.SaveFile;
        if (saveFile != null)
        {
            if (saveFile.Stream != null)
            {
                await r2.WriteFileAsync(saveFile.FilePath, saveFile.Stream, token);
            }
            else if (saveFile.Text != null)
            {
                await r2.WriteFileAsync(saveFile.FilePath, saveFile.Text, token);
            }
            else if (saveFile.Bytes != null)
            {
                await r2.WriteFileAsync(saveFile.FilePath, saveFile.Bytes, token);
            }
        }

        if (request.CdnDeleteFiles != null)
        {
            await r2.DeleteFilesAsync(request.CdnDeleteFiles);
        }
        
        if (request.SaveQuestion != null)
        {
            await ExecUtils.RetryOnExceptionAsync(async () => 
                await questions.SaveQuestionAsync(request.SaveQuestion), 5);
        }
    }
}