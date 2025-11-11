using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;
using SS14.Admin.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace SS14.Admin.Components.Pages.Players;

public partial class Players
{
    [Inject]
    private IDbContextFactory<PostgresServerDbContext>? ContextFactory { get; set; }

    [SupplyParameterFromForm(FormName = "playerFilter")]
    public PlayerFilterModel _model { get; set; } = new();

    public QuickGrid<PlayerViewModel> Grid { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 13 };

    private List<PlayerViewModel> _playersList = new();

    protected override async Task OnInitializedAsync()
    {
        await Refresh();
    }

    private async Task Refresh()
    {
        await using var context = await ContextFactory!.CreateDbContextAsync();
        _playersList = await GetPlayersQuery(context).ToListAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnFilterSubmit(EditContext context)
    {
        await Refresh();
    }

    private IQueryable<PlayerViewModel> GetPlayersQuery(PostgresServerDbContext context)
    {
        var query = context.Player.AsNoTracking().AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(_model.Search))
        {
            var search = _model.Search.ToLower();
            query = query.Where(p =>
                p.LastSeenUserName.ToLower().Contains(search) ||
                EF.Functions.Like(p.UserId.ToString().ToLower(), $"%{search}%") ||
                (p.LastSeenAddress != null && EF.Functions.Like(p.LastSeenAddress.ToString().ToLower(), $"%{search}%")) ||
                (p.LastSeenHWId != null && EF.Functions.Like(p.LastSeenHWId.ToString().ToLower(), $"%{search}%"))
            );
        }

        // Apply first seen date filters
        if (_model.FirstSeenFrom.HasValue)
        {
            query = query.Where(p => p.FirstSeenTime >= _model.FirstSeenFrom.Value);
        }

        if (_model.FirstSeenTo.HasValue)
        {
            var firstSeenTo = _model.FirstSeenTo.Value.AddDays(1);
            query = query.Where(p => p.FirstSeenTime < firstSeenTo);
        }

        // Apply last seen date filters
        if (_model.LastSeenFrom.HasValue)
        {
            query = query.Where(p => p.LastSeenTime >= _model.LastSeenFrom.Value);
        }

        if (_model.LastSeenTo.HasValue)
        {
            var lastSeenTo = _model.LastSeenTo.Value.AddDays(1);
            query = query.Where(p => p.LastSeenTime < lastSeenTo);
        }

        return query
            .OrderBy(p => p.LastSeenUserName)
            .Select(player => new PlayerViewModel
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
            });
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
