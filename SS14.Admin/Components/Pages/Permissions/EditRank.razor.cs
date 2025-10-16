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
    private PostgresServerDbContext? Context { get; set; }

    [Inject]
    private NavigationManager Navigation { get; set; }

    public EditViewModel? Model { get; set; }
    private AdminRank? rankEntity;
    private string? originalName;

    protected override async Task OnInitializedAsync()
    {
        if (RankName == null || Context == null)
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
            // Load existing rank by name
            rankEntity = await Context.AdminRank
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
        if (Model == null || Context == null) return;

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

            Context.AdminRank.Add(rankEntity);
        }
        else
        {
            // Update existing rank
            if (rankEntity == null) return;

            rankEntity.Name = Model.Name;

            // Clear existing flags
            rankEntity.Flags.Clear();

            // Add new flags (ensure uppercase)
            foreach (var flag in Model.Flags)
            {
                rankEntity.Flags.Add(new AdminRankFlag { Flag = flag.ToUpper() });
            }
        }

        await Context.SaveChangesAsync();

        Navigation.NavigateTo("/Permissions/Ranks");
    }

    private async Task HandleDelete()
    {
        if (Context == null || rankEntity == null) return;

        Context.AdminRank.Remove(rankEntity);
        await Context.SaveChangesAsync();

        Navigation.NavigateTo("/Permissions/Ranks");
    }

    public class EditViewModel
    {
        public bool IsNew { get; set; }
        public string Name { get; set; } = "";
        public HashSet<string> Flags { get; set; } = new();
    }
}
