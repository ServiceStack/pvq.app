using AiServer.ServiceModel;
using ServiceStack;

namespace MyApp.ServiceModel;

[SystemJson(UseSystemJson.Never)]
public class CreateAnswerCallback : OpenAiChatResponse, IPost, IReturnVoid 
{
    [ValidateGreaterThan(0)]
    public int PostId { get; set; }

    [ValidateNotEmpty]
    public string UserId { get; set; }
}

[SystemJson(UseSystemJson.Never)]
public class RankAnswerCallback : OpenAiChatResponse, IPost, IReturnVoid
{
    [ValidateGreaterThan(0)]
    public int PostId { get; set; }
    
    [ValidateNotEmpty]
    public string UserId { get; set; } // Use User GUID to prevent tampering
    
    [ValidateNotEmpty]
    public string Grader { get; set; }
}

public class GradeResult
{
    public string Reason { get; set; }
    public int Score { get; set; }
    public void Deconstruct(out string reason, out int score)
    {
        reason = Reason;
        score = Score;
    }
}

[SystemJson(UseSystemJson.Never)]
public class AnswerCommentCallback : OpenAiChatResponse, IPost, IReturnVoid
{
    [ValidateNotEmpty]
    public string AnswerId { get; set; }
    
    [ValidateNotEmpty]
    public string UserId { get; set; } // Use User GUID to prevent tampering
    
    [ValidateNotEmpty]
    public string AiRef { get; set; } // Ref for AI Task that generated the comment
}

public class CreateAnswerTasks
{
    public Post Post { get; set; }
    public List<string> ModelUsers { get; set; } = new();
}

public class CreateRankAnswerTask
{
    public string AnswerId { get; set; }
    public string UserId { get; set; }
}

public class CreateAnswerCommentTask
{
    public string? AiRef { get; set; }
    public string Model { get; set; }
    public Post Question { get; set; }
    public Post Answer { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public List<Comment> Comments { get; set; }
}

public class CreateOpenAiChat : IReturn<CreateOpenAiChatResponse>
{
    public string? RefId { get; set; }
    public string? Provider { get; set; }
    public string? ReplyTo { get; set; }
    public string? Tag { get; set; }
    public OpenAiChat Request { get; set; }
}

public class CreateOpenAiChatResponse
{
    public long Id { get; set; }
    public string RefId { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[ValidateHasRole(Roles.Moderator)]
public class CreateAnswersForModel : IPost, IReturn<StringResponse>
{
    [ValidateNotEmpty]
    public string Model { get; set; }
    
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<int> PostIds { get; set; }
}
