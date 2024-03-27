using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class JobServices(QuestionsProvider questions, ModelWorkerQueue workerQueues) : Service
{
    public object Any(ViewModelQueues request)
    {
        var jobs = workerQueues.GetAll(request.Models);
        return new ViewModelQueuesResponse
        {
            Jobs = jobs
        };
    }

    public async Task<object> Any(GetNextJobs request)
    {
        var to = new GetNextJobsResponse();
        var job = workerQueues.Dequeue(request.Models, TimeSpan.FromSeconds(30));
        if (job == null)
        {
            var dbHasIncompleteJobs = await Db.SelectAsync(Db.From<PostJob>()
                .Where(x => x.CompletedDate == null || x.StartedDate < DateTime.UtcNow.AddMinutes(-5)));
            if (dbHasIncompleteJobs.Count > 0)
            {
                await Any(new RestoreModelQueues { RestoreFailedJobs = true });
                job = workerQueues.Dequeue(request.Models, TimeSpan.FromSeconds(30));
            }
            if (job == null)
            {
                return to;
            }
        }
        
        MessageProducer.Publish(new DbWrites {
            StartJob = new() { Id = job.Id, Worker = request.Worker, WorkerIp = Request!.RemoteIp }
        });
        return new GetNextJobsResponse
        {
            Results = [job]
        };
    }

    public void Any(FailJob request)
    {
        MessageProducer.Publish(new DbWrites {
            FailJob = request
        });
    }

    public async Task<object> Any(RestoreModelQueues request)
    {
        var pendingJobs = workerQueues.GetAll();
        var pendingJobIds = pendingJobs.Select(x => x.Id).ToSet();
        var incompleteJobs = await Db.SelectAsync(Db.From<PostJob>().Where(x => x.CompletedDate == null));
        var missingJobs = incompleteJobs.Where(x => !pendingJobIds.Contains(x.Id) && x.StartedDate == null).ToList();
        var startedJobs = incompleteJobs.Where(x => x.StartedDate != null).ToList();
        var lostJobsBefore = DateTime.UtcNow.Add(TimeSpan.FromMinutes(-5));
        var lostJobs = startedJobs.Where(x => x.StartedDate < lostJobsBefore && missingJobs.All(m => m.Id != x.Id)).ToList();
        var failedJobs = await Db.SelectAsync(Db.From<PostJob>().Where(x => x.CompletedDate != null && x.Error != null));
        var restoreFailedJobs = request.RestoreFailedJobs == true;

        foreach (var lostJob in lostJobs)
        {
            if (pendingJobIds.Contains(lostJob.Id)) continue;
            workerQueues.Enqueue(lostJob);
        }
        foreach (var missingJob in missingJobs)
        {
            if (pendingJobIds.Contains(missingJob.Id)) continue;
            workerQueues.Enqueue(missingJob);
        }
        foreach (var failedJob in failedJobs)
        {
            if (pendingJobIds.Contains(failedJob.Id)) continue;
            workerQueues.Enqueue(failedJob);
        }
        var failedSuffix = restoreFailedJobs ? ", restored!" : "";

        return new StringsResponse
        {
            Results = [
                $"{pendingJobs.Count} pending jobs in queue",
                $"{incompleteJobs.Count} incomplete jobs in database",
                $"{startedJobs.Count} jobs being processed by workers",
                $"{missingJobs.Count} missing and {lostJobs.Count} lost jobs re-added to queue",
                $"{failedJobs.Count} failed jobs{failedSuffix}",
            ]
        };
    }

    // For testing purposes
    public static int JobIdCount;
}