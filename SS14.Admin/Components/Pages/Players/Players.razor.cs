using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;

namespace SS14.Admin.Components.Pages.Players;

public partial class Players
{
    [Inject]
    private PostgresServerDbContext? Context { get; set; }
    public QuickGrid<PlayerViewModel> Grid { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 13 };

    private IQueryable<PlayerViewModel> _playersQuery = Enumerable.Empty<PlayerViewModel>().AsQueryable();

    private IQueryable<PlayerViewModel> GetPlayersQuery() =>
        from player in Context.Player.AsNoTracking()
    orderby player.LastSeenUserName
    select new PlayerViewModel
    {
        Id = player.Id,
        LastSeenUsername = player.LastSeenUserName,
        Guid = player.UserId.ToString(),
        LastSeen = player.LastSeenTime,
        FirstSeen = player.FirstSeenTime,
        LastSeenIPAddress = player.LastSeenAddress.ToString(),
        LastSeenHwid = player.LastSeenHWId != null
            ? player.LastSeenHWId.ToImmutable().ToString()
            : ""
    };

    private async Task Refresh()
    {
        _playersQuery = GetPlayersQuery();
        await InvokeAsync(StateHasChanged);
    }

    public class PlayerViewModel
    {
        public int Id { get; set; }
        public string LastSeenUsername { get; set; } = "";
        public string Guid { get; set; }
        public DateTime LastSeen { get; set; }
        public DateTime FirstSeen { get; set; }
        public string LastSeenIPAddress { get; set; } = "";
        public string LastSeenHwid { get; set; } = "";
    }
}
