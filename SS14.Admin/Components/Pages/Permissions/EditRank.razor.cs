using Microsoft.AspNetCore.Components;

namespace SS14.Admin.Components.Pages.Permissions;

public partial class EditRank : ComponentBase
{
    [Parameter]
    public int? RankId { get; set; }

    protected override void OnInitialized()
    {
        // This is a placeholder page
    }
}
