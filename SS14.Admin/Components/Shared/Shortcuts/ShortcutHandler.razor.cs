using Microsoft.AspNetCore.Components;

namespace SS14.Admin.Components.Shared.Shortcuts;

public partial class ShortcutHandler : ComponentBase
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private Dictionary<string, List<Action>> ShortcutHandlers { get; } = new();
}

