using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SS14.Admin.Components.Shared.Shortcuts;

public partial class ShortcutHandler : ComponentBase
{
    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private Dictionary<string, List<Func<Task>>> ShortcutHandlers { get; } = new();

    private IJSObjectReference? _jsInstance;
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var reference = DotNetObjectReference.Create(this);
            _jsInstance = await Js.InvokeAsync<IJSObjectReference>("ShortcutHandler.Create", reference);
            foreach (var shortcut in ShortcutHandlers.Keys)
            {
                await _jsInstance.InvokeVoidAsync("RegisterShortcut", shortcut);
            }
        }
    }

    [JSInvokable]
    public async Task OnShortcut(string shortcutKey)
    {
        if (!ShortcutHandlers.TryGetValue(shortcutKey, out var handlers))
            return;

        foreach (var action in handlers)
        {
            await action.Invoke();
        }
    }
}

