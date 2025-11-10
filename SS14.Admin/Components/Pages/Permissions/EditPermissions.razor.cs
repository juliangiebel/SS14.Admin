using Microsoft.AspNetCore.Components;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Components.Pages.Permissions;

public partial class EditPermissions : ComponentBase
{
    [Parameter]
    public Guid UserId { get; set; }

    [Inject]
    private IDbContextFactory<PostgresServerDbContext>? ContextFactory { get; set; }

    [Inject]
    private NavigationManager Navigation { get; set; }

    public EditViewModel? Model { get; set; }
    public List<AdminRank> Ranks { get; set; } = new();
    private Content.Server.Database.Admin? adminEntity;

    protected override async Task OnInitializedAsync()
    {
        await using var context = await ContextFactory!.CreateDbContextAsync();

        Ranks = await context.AdminRank.ToListAsync();

        adminEntity = await context.Admin
            .Include(a => a.Flags)
            .Include(a => a.AdminRank)
            .ThenInclude(r => r.Flags)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.UserId == UserId);

        if (adminEntity == null)
        {
            // Handle not found or not im lazy
            return;
        }

        var player = await context.Player.FirstOrDefaultAsync(p => p.UserId == UserId);
        var username = player?.LastSeenUserName ?? "Unknown";

        var posFlags = adminEntity.Flags.Where(f => !f.Negative).Select(f => f.Flag.ToUpper()).ToList();
        var negFlags = adminEntity.Flags.Where(f => f.Negative).Select(f => f.Flag.ToUpper()).ToList();
        var rankFlags = adminEntity.AdminRank?.Flags.Select(f => f.Flag.ToUpper()).ToList() ?? new List<string>();

        Model = new EditViewModel
        {
            UserId = adminEntity.UserId,
            Username = username,
            Title = adminEntity.Title ?? "",
            AdminRankId = adminEntity.AdminRankId,
            RankFlags = rankFlags,
            PosFlags = posFlags,
            NegFlags = negFlags
        };

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
        if (Model == null || ContextFactory == null || adminEntity == null) return;

        await using var context = await ContextFactory.CreateDbContextAsync();
        context.Admin.Attach(adminEntity);

        adminEntity.Title = string.IsNullOrEmpty(Model.Title) ? null : Model.Title;
        adminEntity.AdminRankId = Model.AdminRankId;

        // Clear existing flags
        // This is unfortunatly how it is handled in game now, yes it creates needless more ids
        adminEntity.Flags.Clear();

        // Add new pos flags (ensure uppercase)
        foreach (var flag in Model.PosFlags)
        {
            adminEntity.Flags.Add(new AdminFlag { Flag = flag.ToUpper(), Negative = false });
        }

        // Add new neg flags (ensure uppercase)
        foreach (var flag in Model.NegFlags)
        {
            adminEntity.Flags.Add(new AdminFlag { Flag = flag.ToUpper(), Negative = true });
        }

        await context.SaveChangesAsync();

        Navigation.NavigateTo("/Permissions");
    }

    public class EditViewModel
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = "";
        public string Title { get; set; } = "";
        public int? AdminRankId { get; set; }
        public List<string> RankFlags { get; set; } = new();
        public List<string> PosFlags { get; set; } = new();
        public List<string> NegFlags { get; set; } = new();
    }
}
