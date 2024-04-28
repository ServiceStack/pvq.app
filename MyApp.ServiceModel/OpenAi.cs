using AiServer.ServiceModel;
using ServiceStack;

namespace MyApp.ServiceModel;

[SystemJson(UseSystemJson.Never)]
public class CreateAnswerCallback : OpenAiChatResponse, IPost, IReturnVoid 
{
    public int PostId { get; set; }
    
    public string UserId { get; set; }
}

public class RankAnswerCallback : OpenAiChatResponse, IPost, IReturnVoid
{
}

public class CreateRankAnswerTask
{
    public string AnswerId { get; set; }
}

public class CreateOpenAiChat : IReturn<CreateOpenAiChatResponse>
{
    public string? RefId { get; set; }
    [ValidateNotEmpty]
    public string Model { get; set; }
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
