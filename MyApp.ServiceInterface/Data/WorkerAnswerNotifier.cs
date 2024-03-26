using Microsoft.Extensions.Logging;

namespace MyApp.Data;

public class WorkerAnswerNotifier(ILogger<WorkerAnswerNotifier> log)
{
    long counter = 0;
    public long Counter => Interlocked.Read(ref counter);

    public void NotifyNewAnswer(int postId, string userName)
    {
        log.LogDebug("{Counter}: Notifying of new answer by {UserName} for {PostId}", Counter, userName, postId);
        Interlocked.Increment(ref counter);
    }

    public async Task<bool> ListenForNewAnswersAsync(int postId, TimeSpan timeOut)
    {
        var initialCount = Counter;
        var startedAt = DateTime.UtcNow;
        log.LogDebug("{Counter}: Waiting on new answer for {PostId}...", initialCount, postId);
        
        while (DateTime.UtcNow - startedAt < timeOut)
        {
            if (Counter > initialCount)
            {
                log.LogDebug("{Counter}: Notified of new answer", Counter);
                return true;
            }
            await Task.Delay(50);
        }

        log.LogDebug("{Counter}: Timed out waiting for new answer", Counter);
        return false;
    }
}
