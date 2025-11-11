using Microsoft.AspNetCore.Components;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace SS14.Admin.Components.Pages.Permissions;

public partial class AddAdmin : ComponentBase
{
    [Inject]
    private IDbContextFactory<PostgresServerDbContext>? ContextFactory { get; set; }

    [Inject]
    private NavigationManager Navigation { get; set; }

    public AddAdminViewModel? Model { get; set; }
    public List<AdminRank> Ranks { get; set; } = new();
    public bool IsLoading { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        await using var context = await ContextFactory!.CreateDbContextAsync();

        // Load all ranks
        Ranks = await context.AdminRank.ToListAsync();

        Model = new AddAdminViewModel
        {
            UserIdString = "",
            Title = "",
            AdminRankId = null,
            RankFlags = new List<string>(),
            PosFlags = new List<string>(),
            NegFlags = new List<string>()
        };

        IsLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnInheritedFlagChanged(string flagName)
    {
        if (Model == null) return;

        var upperFlagName = flagName.ToUpper();

        // Remove both positive and negative flags to return to inherited state
        Model.PosFlags.Remove(upperFlagName);
        Model.NegFlags.Remove(upperFlagName);

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnPositiveFlagChanged(string flagName)
    {
        if (Model == null) return;

        var upperFlagName = flagName.ToUpper();

        // Add positive flag and remove negative flag
        if (!Model.PosFlags.Contains(upperFlagName))
        {
            Model.PosFlags.Add(upperFlagName);
        }
        Model.NegFlags.Remove(upperFlagName);

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnNegativeFlagChanged(string flagName)
    {
        if (Model == null) return;

        var upperFlagName = flagName.ToUpper();

        // Add negative flag and remove positive flag
        if (!Model.NegFlags.Contains(upperFlagName))
        {
            Model.NegFlags.Add(upperFlagName);
        }
        Model.PosFlags.Remove(upperFlagName);

        await InvokeAsync(StateHasChanged);
    }

    private async Task UpdateRankFlags()
    {
        if (Model == null) return;
        var rank = Ranks.FirstOrDefault(r => r.Id == Model.AdminRankId);
        Model.RankFlags = rank?.Flags.Select(f => f.Flag.ToUpper()).ToList() ?? new List<string>();
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleValidSubmit()
    {
        if (Model == null || ContextFactory == null) return;

        // Parse GUID from string
        if (!Guid.TryParse(Model.UserIdString, out var userId))
        {
            // Invalid GUID format - validation should catch this but just in case
            return;
        }

        await using var context = await ContextFactory.CreateDbContextAsync();

        // Check if admin already exists
        var existingAdmin = await context.Admin.FirstOrDefaultAsync(a => a.UserId == userId);
        if (existingAdmin != null)
        {
            // Admin already exists, redirect back
            Navigation.NavigateTo("/Permissions");
            return;
        }

        // Create new admin entity
        var newAdmin = new Content.Server.Database.Admin
        {
            UserId = userId,
            Title = string.IsNullOrEmpty(Model.Title) ? null : Model.Title,
            AdminRankId = Model.AdminRankId,
            Flags = new List<AdminFlag>()
        };

        // Add positive flags (ensure uppercase)
        foreach (var flag in Model.PosFlags)
        {
            newAdmin.Flags.Add(new AdminFlag { Flag = flag.ToUpper(), Negative = false });
        }

        // Add negative flags (ensure uppercase)
        foreach (var flag in Model.NegFlags)
        {
            newAdmin.Flags.Add(new AdminFlag { Flag = flag.ToUpper(), Negative = true });
        }

        context.Admin.Add(newAdmin);
        await context.SaveChangesAsync();

        Navigation.NavigateTo("/Permissions");
    }

    public class AddAdminViewModel
    {
        [Required(ErrorMessage = "User ID is required")]
        [RegularExpression(@"^[{]?[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}[}]?$",
            ErrorMessage = "Invalid GUID format")]
        public string UserIdString { get; set; } = "";

        public string Title { get; set; } = "";
        public int? AdminRankId { get; set; }
        public List<string> RankFlags { get; set; } = new();
        public List<string> PosFlags { get; set; } = new();
        public List<string> NegFlags { get; set; } = new();
    }
}
