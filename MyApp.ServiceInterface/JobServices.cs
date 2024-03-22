using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class JobServices(QuestionsProvider QuestionsProvider) : Service
{
    public async Task<object> Get(CheckPostJobs request)
    {
        // Place holder for the actual implementation
        var post = Db.Single<Post>(x => x.Id == 105372);
        JobIdCount++;
        var questionFile = await QuestionsProvider.GetQuestionAsync(post.Id);
        var question = await questionFile.GetQuestionAsync();
        
        var result = new List<PostJob>
        {
            new PostJob
            {
                JobId = JobIdCount,
                Body = question?.Post.Body,
                Tags = post.Tags,
                Title = post.Title,
                PostId = post.Id
            }
        };
        
        return new CheckPostJobsResponse { Results = result };
    }

    // For testing purposes
    public static int JobIdCount;
}