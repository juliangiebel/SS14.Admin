using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SS14.Admin.Components.Forms;

public partial class DatePicker : ComponentBase, IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _instance;

    [Parameter] public string? Label { get; set; }
    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }


    private readonly string _id = $"date-picker-{Guid.NewGuid().ToString()}";

    public DatePicker(IJSRuntime js)
    {
        _js = js;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        var module = await _js.InvokeAsync<IJSObjectReference>("import", "./Components/Forms/DatePicker.razor.js");

        var options = new
        {
            rangeStart = DateTime.Now,
        };

        _instance = await module.InvokeAsync<IJSObjectReference>("DatePicker.init", DotNetObjectReference.Create(this), _id, options);
        await module.DisposeAsync();
    }


    public async ValueTask DisposeAsync()
    {
        if (_instance is null)
            return;

        try
        {
            await _instance.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}

