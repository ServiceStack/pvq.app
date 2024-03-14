using ServiceStack;

namespace MyApp.ServiceModel;

public class RenderHome
{
    public string? Tab { get; set; }
    public List<Post> Posts { get; set; }
}

public class RenderComponent : IReturnVoid
{
    public int? IfQuestionModified { get; set; }
    public QuestionAndAnswers? Question { get; set; }
    public RenderHome? Home { get; set; }
}