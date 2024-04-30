﻿using AiServer.ServiceModel;
using ServiceStack;

namespace MyApp.ServiceModel;

[SystemJson(UseSystemJson.Never)]
public class CreateAnswerCallback : OpenAiChatResponse, IPost, IReturnVoid 
{
    public int PostId { get; set; }
    
    public string UserId { get; set; }
}

[SystemJson(UseSystemJson.Never)]
public class RankAnswerCallback : OpenAiChatResponse, IPost, IReturnVoid
{
    public int PostId { get; set; }
    
    public string UserId { get; set; } // Use User GUID to prevent tampering
    
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

public class CreateOpenAiChat : IReturn<CreateOpenAiChatResponse>
{
    public string? RefId { get; set; }
    public string? Provider { get; set; }
    public string? ReplyTo { get; set; }
    public OpenAiChat Request { get; set; }
}

public class CreateOpenAiChatResponse
{
    public long Id { get; set; }
    public string RefId { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}