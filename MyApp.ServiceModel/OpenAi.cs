﻿using AiServer.ServiceModel;
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


public class QueueOpenAiChatCompletion : IReturn<QueueOpenAiChatResponse>
{
    public string? RefId { get; set; }
    public string? Provider { get; set; }
    public string? ReplyTo { get; set; }
    public string? Tag { get; set; }
    public OpenAiChat Request { get; set; }
}
public class QueueOpenAiChatResponse
{
    public long Id { get; set; }
    public string RefId { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[ValidateHasRole(Roles.Moderator)]
public class CreateAnswersForModels : IPost, IReturn<CreateAnswersForModelsResponse>
{
    [ValidateNotEmpty]
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<string> Models { get; set; }
    
    [ValidateNotEmpty]
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<int> PostIds { get; set; }
}
public class CreateAnswersForModelsResponse
{
    public Dictionary<int, string> Errors { get; set; } = new();
    public List<int> Results { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

[ValidateHasRole(Roles.Moderator)]
public class CreateRankingTasks : IPost, IReturn<CreateRankingTasksResponse>
{
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<string> AnswerIds { get; set; }
}
public class CreateRankingTasksResponse
{
    public Dictionary<string, string> Errors { get; set; } = new();
    public List<string> Results { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}
