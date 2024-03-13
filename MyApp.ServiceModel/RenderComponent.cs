using ServiceStack;

namespace MyApp.ServiceModel;

public class RenderComponent : IReturnVoid
{
    public int? IfQuestionModified { get; set; }
    public QuestionAndAnswers? Question { get; set; }
}