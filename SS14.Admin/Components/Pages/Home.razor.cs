using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace SS14.Admin.Components.Pages;

public partial class Home : ComponentBase
{
    [CascadingParameter] private Task<AuthenticationState>? AuthState { get; set; }
    private ClaimsPrincipal User { get; set; }

    protected override async Task OnInitializedAsync()
    {
        User = (await AuthState!).User;
    }
}
