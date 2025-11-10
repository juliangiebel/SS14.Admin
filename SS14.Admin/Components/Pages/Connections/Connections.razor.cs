using Content.Server.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Models;

namespace SS14.Admin.Components.Pages.Connections;

public partial class Connections
{
    [Inject]
    private IDbContextFactory<PostgresServerDbContext>? ContextFactory { get; set; }

    private readonly ConnectionsFilterModel _filter = new();
    public QuickGrid<ConnectionViewModel>? Grid { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 50 };

    private GridItemsProvider<ConnectionViewModel>? _connectionsProvider;

    protected override async Task OnInitializedAsync()
    {
        _connectionsProvider = async request =>
        {
            await using var context = await ContextFactory!.CreateDbContextAsync();

            // Increase the count by one if it's not unlimited so we can check if there is a next page available
            var limit = request.Count + 1;
            var query = ConnectionsQuery(context);
            query = request.ApplySorting(query);
            query = query.Skip(request.StartIndex);

            if (limit != null)
                query = query.Take(limit.Value);

            var page = await query.ToListAsync();

            if (page.Count == 0)
                return GridItemsProviderResult.From(page, request.StartIndex);

            // We assume that there's at least another page worth of items left if the amount of returned items
            // is more than the requested amount.
            var hasNextPage = request.Count != null && page.Count > request.Count;

            // Return the current count plus twice the returned items to signify that there is at least one more page.
            // If there is no next page, we return the current count plus the returned amount of items once.
            // This total item count mustn't be shown to the user but be used to decide if the next button gets disabled.
            var totalItemCount = request.StartIndex + (hasNextPage ? (page.Count - 1) * 2 : page.Count - 1);

            return GridItemsProviderResult.From(page, totalItemCount);
        };
    }

    private IQueryable<ConnectionViewModel> ConnectionsQuery(PostgresServerDbContext context)
    {
        var acceptableDenies = new List<ConnectionDenyReason?>();
        if (_filter.ShowAccepted)
            acceptableDenies.Add(null);
        if (_filter.ShowBanned)
            acceptableDenies.Add(ConnectionDenyReason.Ban);
        if (_filter.ShowWhitelist)
            acceptableDenies.Add(ConnectionDenyReason.Whitelist);
        if (_filter.ShowFull)
            acceptableDenies.Add(ConnectionDenyReason.Full);
        if (_filter.ShowPanic)
            acceptableDenies.Add(ConnectionDenyReason.Panic);
        if (_filter.ShowBabyJail)
            acceptableDenies.Add(ConnectionDenyReason.BabyJail);
        if (_filter.ShowIPChecks)
            acceptableDenies.Add(ConnectionDenyReason.IPChecks);

        // Start with the base query
        var connectionQuery = context.ConnectionLog
            .AsNoTracking()
            .Include(c => c.Server)
            .Include(c => c.BanHits)
            .Where(c => acceptableDenies.Contains(c.Denied));

        // Apply filters on the entity before projection
        if (!string.IsNullOrWhiteSpace(_filter.Search))
        {
            var searchUpper = _filter.Search.ToUpper();
            connectionQuery = connectionQuery.Where(c =>
                c.UserName.ToUpper().Contains(searchUpper) ||
                EF.Functions.Like(c.UserId.ToString(), $"%{_filter.Search}%")
            );
        }

        if (_filter.DateFrom != null)
        {
            connectionQuery = connectionQuery.Where(c => c.Time >= _filter.DateFrom);
        }

        if (_filter.DateTo != null)
        {
            connectionQuery = connectionQuery.Where(c => c.Time <= _filter.DateTo);
        }

        if (_filter.ServerId != null)
        {
            connectionQuery = connectionQuery.Where(c => c.ServerId == _filter.ServerId);
        }

        // Now project to the view model
        var query = from connection in connectionQuery
            join player in context.Player on connection.UserId equals player.UserId into playerJoin
            from p in playerJoin.DefaultIfEmpty()
            select new ConnectionViewModel
            {
                Id = connection.Id,
                UserName = connection.UserName,
                UserId = connection.UserId,
                Address = connection.Address != null ? connection.Address.ToString() : "",
                HWId = connection.HWId != null ? connection.HWId.ToString() : "",
                Time = connection.Time,
                ServerName = connection.Server.Name,
                ServerId = connection.ServerId,
                Denied = connection.Denied,
                BanHitCount = connection.BanHits.Count,
                PlayerLastSeenName = p != null ? p.LastSeenUserName : null
            };

        return query;
    }

    private async Task Refresh()
    {
    }

    private async Task RefreshFilter()
    {
        await Grid!.RefreshDataAsync();
    }

    public class ConnectionViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = "";
        public Guid UserId { get; set; }
        public string Address { get; set; } = "";
        public string HWId { get; set; } = "";
        public DateTime Time { get; set; }
        public string ServerName { get; set; } = "";
        public int ServerId { get; set; }
        public ConnectionDenyReason? Denied { get; set; }
        public int BanHitCount { get; set; }
        public string? PlayerLastSeenName { get; set; }
    }
}
