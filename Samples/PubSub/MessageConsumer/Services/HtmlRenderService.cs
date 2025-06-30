using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;

namespace MessageConsumer.Services;

public class HtmlRenderService(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
{
    private readonly HtmlRenderer _htmlRenderer = new HtmlRenderer(serviceProvider, loggerFactory);
    
    public Task<string> RenderComponent<T>() where T : IComponent
        => RenderComponent<T>(ParameterView.Empty);

    public Task<string> RenderComponent<T>(Dictionary<string, object?> dictionary) where T : IComponent
        => RenderComponent<T>(ParameterView.FromDictionary(dictionary));

    private Task<string> RenderComponent<T>(ParameterView parameters) where T : IComponent
    {
        return _htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            HtmlRootComponent output = await _htmlRenderer.RenderComponentAsync<T>(parameters);
            return output.ToHtmlString();
        });
    }
}