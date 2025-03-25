using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;

namespace SS14.Admin.Components.Shared;

public partial class Pagination : ComponentBase
{
    [Parameter] public bool ShowPerPage { get; set; } = true;
    [Parameter] public EventCallback OnRefreshRequired { get; set; }

    [Parameter, EditorRequired] public PaginationState State { get; set; } = default!;

    [Parameter] public string Class { get; set; } = default!;

    [PublicAPI]
    public Task GoToPageAsync(int pageIndex)
        => State.SetCurrentPageIndexAsync(pageIndex);

    protected override void OnParametersSet()
    {
        State.TotalItemCountChanged += ItemCountChanged;
    }

    private Task GoFirstAsync() => GoToPageAsync(0);
    private Task GoPreviousAsync() => GoToPageAsync(Math.Max(0, State.CurrentPageIndex - 1));
    private Task GoNextAsync() => GoToPageAsync(Math.Min(State.CurrentPageIndex + 1, State.LastPageIndex ?? int.MaxValue));
    private Task GoLastAsync() => GoToPageAsync(State.LastPageIndex.GetValueOrDefault(0));
    private bool CanGoBack => State.CurrentPageIndex > 0;
    private bool CanGoForwards => State.CurrentPageIndex < State.LastPageIndex;
    private ElementReference PerPageInput { get; set; }

    private void ItemCountChanged(object? sender, int? e)
    {
        StateHasChanged();
    }

    private async Task OnShortcutLeft()
    {
        await GoPreviousAsync();
        StateHasChanged();
    }

    private async Task OnShortcutRight()
    {
        await GoNextAsync();
        StateHasChanged();
    }

    private async Task Refresh()
    {
        if (!OnRefreshRequired.HasDelegate)
            return;

        await OnRefreshRequired.InvokeAsync();
    }

    private async Task PerPageChanged()
    {
        await Refresh();
    }
}

