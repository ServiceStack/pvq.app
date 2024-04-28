using System.Collections.Concurrent;
using MyApp.ServiceModel;

namespace MyApp.Data;

public class ModelWorkerQueue
{
    Dictionary<string, BlockingCollection<PostJob>> queues = new()
    {
        ["phi"] = new(),
        ["mistral"] = new(),
        ["gemma"] = new(),
        ["gemini-pro"] = new(),
        ["codellama"] = new(),
        ["mixtral"] = new(),
        ["gpt3.5-turbo"] = new(),
        ["claude3-haiku"] = new(),
        ["command-r"] = new(),
        ["wizardlm"] = new(),
        ["claude3-sonnet"] = new(),
        ["command-r-plus"] = new(),
        ["gpt4-turbo"] = new(),
        ["claude3-opus"] = new(),
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
