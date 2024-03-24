using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class JobServices(QuestionsProvider questions, ModelWorkerQueue workerQueues) : Service
{
    public async Task<object> Get(CheckPostJobs request)
    {
        // Place holder for the actual implementation
        var post = Db.Single<Post>(x => x.Id == 105372);
        JobIdCount++;
        var questionFile = await questions.GetQuestionAsync(post.Id);
        var question = await questionFile.GetQuestionAsync();
        
        var result = new List<PostJob>
        {
            new PostJob
            {
                Id = JobIdCount,
                Body = question?.Post.Body,
                Tags = post.Tags,
                Title = post.Title,
                PostId = post.Id
            }
        };
        
        return new CheckPostJobsResponse { Results = result };
    }

    public object Any(ViewModelQueues request)
    {
        var jobs = workerQueues.GetAll(request.Models);
        return new ViewModelQueuesResponse
        {
            Jobs = jobs
        };
    }

    public object Any(GetNextJobs request)
    {
        var to = new GetNextJobsResponse();
        var job = workerQueues.Dequeue(request.Models, TimeSpan.FromSeconds(30));
        if (job == null)
            return to;
        
        MessageProducer.Publish(new DbWrites {
            StartJob = new() { Id = job.Id, Worker = request.Worker, WorkerIp = Request?.RemoteIp }
        });
        return new GetNextJobsResponse
        {
            Results = [job]
        };
    }

    public object Any(RestoreModelQueues request)
    {
        var pendingJobs = workerQueues.GetAll();
        var pendingJobIds = pendingJobs.Select(x => x.Id).ToSet();
        var incompleteJobs = Db.Select(Db.From<PostJob>().Where(x => x.CompletedDate == null));
        var missingJobs = incompleteJobs.Where(x => !pendingJobIds.Contains(x.Id) && x.StartedDate == null).ToList();
        var startedJobs = incompleteJobs.Where(x => x.StartedDate != null).ToList();

        foreach (var missingJob in missingJobs)
        {
            workerQueues.Enqueue(missingJob);
        }

        return new StringsResponse
        {
            Results = [
                $"{pendingJobs.Count} pending jobs in queue",
                $"{incompleteJobs.Count} incomplete jobs in database",
                $"{startedJobs.Count} jobs being processed by workers",
                $"{missingJobs.Count} jobs missing and re-added to queue",
            ]
        };
    }

    // For testing purposes
    public static int JobIdCount;
}