namespace MyApp.ServiceInterface;

public static class OpenAiExtensions
{
    public static string? GetModelAnswerBody(this Dictionary<string, object> obj)
    {
        if (!obj.TryGetValue("choices", out var oChoices) || oChoices is not List<object> choices) 
            return null;
        if (choices.Count <= 0 || choices[0] is not Dictionary<string, object> choice) 
            return null;
        if (choice["message"] is Dictionary<string, object> message)
            return message["content"] as string;
        return null;
    }
}
