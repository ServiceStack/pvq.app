using System.Collections.Concurrent;
using MyApp.ServiceModel;

namespace MyApp.Data;

public class ModelWorkerQueue
{
    Dictionary<string, BlockingCollection<PostJob>> queues = new()
    {
        ["phi"] = new(),
        ["gemma:2b"] = new(),
        ["qwen:4b"] = new(),
        ["codellama"] = new(),
        ["gemma"] = new(),
        ["deepseek-coder:6.7b"] = new(),
        ["mistral"] = new(),
        ["mixtral"] = new(),
        ["rank"] = new(),
    };

    public void Enqueue(PostJob job)
    {
        if (!queues.TryGetValue(job.Model, out var queue))
            throw new NotSupportedException($"Model '{job.Model}' is not supported");

        queue.Add(job);
    }

    public PostJob? Dequeue(IEnumerable<string> models, TimeSpan timeOut)
    {
        var modelQueues = models.Select(model =>
            this.queues.TryGetValue(model, out var queue) 
                ? queue 
                : throw new NotSupportedException($"Model '{model}' is not supported")).ToArray();

        return BlockingCollection<PostJob>.TryTakeFromAny(modelQueues, out var postJob, timeOut) >= 0
            ? postJob
            : null;
    }

    public List<PostJob> GetAll(List<string>? models = null)
    {
        var to = new List<PostJob>();
        if (models == null || models.Count == 0)
        {
            models = queues.Keys.ToList();
        }
        foreach (var model in models)
        {
            if (queues.TryGetValue(model, out var modelQueue))
            {
                foreach (var job in modelQueue)
                {
                    to.Add(job);
                }
            }
        }
        return to;
    }
}
