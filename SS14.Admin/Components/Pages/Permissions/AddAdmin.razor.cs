using Microsoft.AspNetCore.Components;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Components.Pages.Permissions;

public partial class AddAdmin : ComponentBase
{
    [Inject]
    private IDbContextFactory<PostgresServerDbContext>? ContextFactory { get; set; }

    [Inject]
    private NavigationManager Navigation { get; set; }

    public AddAdminViewModel? Model { get; set; }
    public List<AdminRank> Ranks { get; set; } = new();
    public List<Player> AvailablePlayers { get; set; } = new();
    public bool IsLoading { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        await using var context = await ContextFactory!.CreateDbContextAsync();

        // Load all ranks
        Ranks = await context.AdminRank.ToListAsync();

        // Load all players who are not already admins
        var existingAdminUserIds = await context.Admin.Select(a => a.UserId).ToListAsync();
        AvailablePlayers = await context.Player
            .Where(p => !existingAdminUserIds.Contains(p.UserId))
            .OrderBy(p => p.LastSeenUserName)
            .ToListAsync();

        Model = new AddAdminViewModel
        {
            UserId = Guid.Empty,
            Title = "",
            AdminRankId = null,
            RankFlags = new List<string>(),
            PosFlags = new List<string>(),
            NegFlags = new List<string>()
        };

        IsLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task UpdateSelectedPlayer()
    {
        // This is called when a player is selected from the dropdown
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
        if (Model == null || ContextFactory == null || Model.UserId == Guid.Empty) return;

        await using var context = await ContextFactory.CreateDbContextAsync();

        // Check if admin already exists (shouldn't happen but safety check)
        var existingAdmin = await context.Admin.FirstOrDefaultAsync(a => a.UserId == Model.UserId);
        if (existingAdmin != null)
        {
            // Admin already exists, redirect back
            Navigation.NavigateTo("/Permissions");
            return;
        }

        // Create new admin entity
        var newAdmin = new Content.Server.Database.Admin
        {
            UserId = Model.UserId,
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
        public Guid UserId { get; set; }
        public string Title { get; set; } = "";
        public int? AdminRankId { get; set; }
        public List<string> RankFlags { get; set; } = new();
        public List<string> PosFlags { get; set; } = new();
        public List<string> NegFlags { get; set; } = new();
    }
}
