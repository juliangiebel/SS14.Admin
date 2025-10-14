using System.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Components.Pages.Permissions;

public partial class Permissions : ComponentBase
{
    [Inject]
    private PostgresServerDbContext? Context { get; set; }

    public QuickGrid<AdminViewModel> Grid { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 13 };

    private IQueryable<AdminViewModel> _adminQuery = Enumerable.Empty<AdminViewModel>().AsQueryable();

    protected override async Task OnInitializedAsync()
    {
        await Refresh();
    }
    private async Task Refresh()
    {
        _adminQuery = GetAdminQuery();
        await InvokeAsync(StateHasChanged);
    }
    private IQueryable<AdminViewModel> GetAdminQuery() =>
        from admin in Context!.Admin.AsNoTracking()
        join player in Context.Player.AsNoTracking() on admin.UserId equals player.UserId
        join rank in Context.AdminRank.AsNoTracking() on admin.AdminRankId equals rank.Id into rankJoin
        from r in rankJoin.DefaultIfEmpty()
        orderby player.LastSeenUserName
        select new AdminViewModel
        {
            UserId = admin.UserId,
            Username = player.LastSeenUserName,
            Title = admin.Title ?? "none",
            Rank = r != null ? r.Name : null
        };

    public class AdminViewModel
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = "";
        public string Title { get; set; } = "none";
        public string? Rank { get; set; }

        public Dictionary<string, bool> Flags { get; set; } = new();
    }

}
