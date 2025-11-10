using Microsoft.AspNetCore.Components;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Admins;

namespace SS14.Admin.Components.Pages.Permissions;

public partial class EditRank : ComponentBase
{
    [Parameter]
    public string? RankName { get; set; }

    [Inject]
    private IDbContextFactory<PostgresServerDbContext>? ContextFactory { get; set; }

    [Inject]
    private NavigationManager Navigation { get; set; }

    public EditViewModel? Model { get; set; }
    private AdminRank? rankEntity;
    private string? originalName;

    protected override async Task OnInitializedAsync()
    {
        if (RankName == null || ContextFactory == null)
            return;

        // Check if this is a new rank
        if (RankName.Equals("new", StringComparison.OrdinalIgnoreCase))
        {
            // Create new rank model
            Model = new EditViewModel
            {
                IsNew = true,
                Name = "New Rank",
                Flags = new HashSet<string>()
            };
        }
        else
        {
            await using var context = await ContextFactory.CreateDbContextAsync();

            // Load existing rank by name
            rankEntity = await context.AdminRank
                .Include(r => r.Flags)
                .FirstOrDefaultAsync(r => r.Name == RankName);

            if (rankEntity == null)
            {
                // Rank not found
                return;
            }

            originalName = rankEntity.Name;
            var flags = rankEntity.Flags.Select(f => f.Flag.ToUpper()).ToHashSet();

            Model = new EditViewModel
            {
                IsNew = false,
                Name = rankEntity.Name,
                Flags = flags
            };
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnFlagChanged(string flagName, bool isChecked)
    {
        if (Model == null) return;

        var upperFlagName = flagName.ToUpper();

        if (isChecked)
        {
            Model.Flags.Add(upperFlagName);
        }
        else
        {
            Model.Flags.Remove(upperFlagName);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleValidSubmit()
    {
        if (Model == null || ContextFactory == null) return;

        await using var context = await ContextFactory.CreateDbContextAsync();

        if (Model.IsNew)
        {
            // Create new rank
            rankEntity = new AdminRank
            {
                Name = Model.Name,
                Flags = new List<AdminRankFlag>()
            };

            // Add flags (ensure uppercase)
            foreach (var flag in Model.Flags)
            {
                rankEntity.Flags.Add(new AdminRankFlag { Flag = flag.ToUpper() });
            }

            context.AdminRank.Add(rankEntity);
        }
        else
        {
            // Update existing rank
            if (rankEntity == null) return;

            // Re-attach the entity to the new context
            context.AdminRank.Attach(rankEntity);

            rankEntity.Name = Model.Name;

            // Clear existing flags
            rankEntity.Flags.Clear();

            // Add new flags (ensure uppercase)
            foreach (var flag in Model.Flags)
            {
                rankEntity.Flags.Add(new AdminRankFlag { Flag = flag.ToUpper() });
            }
        }

        await context.SaveChangesAsync();

        Navigation.NavigateTo("/Permissions/Ranks");
    }

    private async Task HandleDelete()
    {
        if (ContextFactory == null || rankEntity == null) return;

        await using var context = await ContextFactory.CreateDbContextAsync();
        context.AdminRank.Attach(rankEntity);
        context.AdminRank.Remove(rankEntity);
        await context.SaveChangesAsync();

        Navigation.NavigateTo("/Permissions/Ranks");
    }

    public class EditViewModel
    {
        public bool IsNew { get; set; }
        public string Name { get; set; } = "";
        public HashSet<string> Flags { get; set; } = new();
    }
}
