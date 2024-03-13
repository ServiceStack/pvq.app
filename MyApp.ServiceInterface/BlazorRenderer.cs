using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace MyApp.ServiceInterface;

public class BlazorRenderer(HtmlRenderer htmlRenderer)
{
    public Task<string> RenderComponent<T>() where T : IComponent
        => RenderComponent<T>(ParameterView.Empty);

    public Task<string> RenderComponent<T>(Dictionary<string, object?> dictionary) where T : IComponent
        => RenderComponent<T>(ParameterView.FromDictionary(dictionary));

    private Task<string> RenderComponent<T>(ParameterView parameters) where T : IComponent
    {
        return htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await htmlRenderer.RenderComponentAsync<T>(parameters);
            return output.ToHtmlString();
        });
    }
}