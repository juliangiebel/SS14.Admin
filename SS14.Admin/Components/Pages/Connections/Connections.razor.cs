using Content.Server.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;
using SS14.Admin.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using SS14.Admin.Services;
using Microsoft.AspNetCore.WebUtilities;

namespace SS14.Admin.Components.Pages.Connections;

public partial class Connections : IDisposable
{
    [Inject]
    private IDbContextFactory<PostgresServerDbContext>? ContextFactory { get; set; }

    [Inject]
    private AuthenticationStateProvider? AuthenticationStateProvider { get; set; }

    [Inject]
    private ClientPreferencesService? ClientPreferences { get; set; }

    [Inject]
    private IFilterKeyService? FilterKeyService { get; set; }

    [Inject]
    private NavigationManager? Navigation { get; set; }

    [Inject]
    private IPiiRedactor? PiiRedactor { get; set; }

    private ClaimsPrincipal? _user;
    private bool _shouldCensorPii;

    // Store filterKey search separately to avoid exposing PII in the search box UI
    private string? _filterKeySearch;

    private readonly ConnectionsFilterModel _filter = new();
    public QuickGrid<ConnectionViewModel>? Grid { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 50 };

    private GridItemsProvider<ConnectionViewModel>? _connectionsProvider;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider!.GetAuthenticationStateAsync();
        _user = authState.User;

        // Initially censor PII until we can load client preferences
        var hasPiiPermission = _user.IsInRole(Constants.PIIRole);
        _shouldCensorPii = !hasPiiPermission;

        // Check for filterKey in query parameters
        var uri = new Uri(Navigation!.Uri);
        var queryParams = QueryHelpers.ParseQuery(uri.Query);

        if (queryParams.TryGetValue("fk", out var filterKeyValue) && !string.IsNullOrWhiteSpace(filterKeyValue))
        {
            var userId = _user.Identity?.Name ?? "unknown";
            var filterCriteria = FilterKeyService!.GetFilterCriteria(filterKeyValue, userId);

            if (filterCriteria != null)
            {
                // Apply filter criteria to the filter model
                ApplyFilterCriteria(filterCriteria);
            }
        }

        ClientPreferences!.OnChange += OnPreferencesChanged;

        _connectionsProvider = async request =>
        {
            await using var context = await ContextFactory!.CreateDbContextAsync();

            // First, create a query that projects to ConnectionViewModel with ToString() for compatibility with SQL
            var baseQuery = ConnectionsQuery(context);

            // Apply sorting and paging
            var query = request.ApplySorting(baseQuery);
            query = query.Skip(request.StartIndex);

            // Increase the count by one if it's not unlimited so we can check if there is a next page available
            var limit = request.Count + 1;
            if (limit != null)
                query = query.Take(limit.Value);

            var page = await query.ToListAsync();

            if (page.Count == 0)
                return GridItemsProviderResult.From(page, request.StartIndex);

            // Now fix the HWID formatting by reloading the entities and reformatting
            var connectionIds = page.Select(p => p.Id).ToList();
            var connections = await context.ConnectionLog
                .AsNoTracking()
                .Include(c => c.Server)
                .Include(c => c.BanHits)
                .Where(c => connectionIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);

            // Update HWID formatting
            foreach (var item in page)
            {
                if (connections.TryGetValue(item.Id, out var connection) && connection.HWId != null)
                {
                    item.HWId = connection.HWId.ToImmutable().ToString();
                }
            }

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Now we can safely call JavaScript interop to get client preferences
            var hasPiiPermission = _user?.IsInRole(Constants.PIIRole) ?? false;
            var clientPrefs = await ClientPreferences!.GetClientPreferences();
            _shouldCensorPii = !hasPiiPermission || clientPrefs.censorPii;
            await InvokeAsync(StateHasChanged);
        }
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

        // Handle FilterKey search (IP/UserId) vs manual search (Username/GUID) differently
        if (!string.IsNullOrWhiteSpace(_filterKeySearch))
        {
            // FilterKey search - try to match IP address or UserId
            var searchTerm = _filterKeySearch;

            // Try to parse as IP address
            if (System.Net.IPAddress.TryParse(searchTerm, out var ipAddress))
            {
                // Search by IP address directly
                connectionQuery = connectionQuery.Where(c => c.Address == ipAddress);
            }
            // Try to parse as GUID
            else if (Guid.TryParse(searchTerm, out var userGuid))
            {
                // Search by UserId - this is used for HWID links (since HWIDs can't be queried in EF)
                connectionQuery = connectionQuery.Where(c => c.UserId == userGuid);
            }
        }
        else if (!string.IsNullOrWhiteSpace(_filter.Search))
        {
            // Manual search from form - search username and userId only
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

        // Project to view model (HWID will be fixed after loading)
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

    private void OnPreferencesChanged(ClientPreferencesService.ClientPreferences preferences)
    {
        var hasPiiPermission = _user?.IsInRole(Constants.PIIRole) ?? false;
        _shouldCensorPii = !hasPiiPermission || preferences.censorPii;
        InvokeAsync(StateHasChanged);
    }

    private async Task RefreshFilter()
    {
        await Grid!.RefreshDataAsync();
    }

    /// <summary>
    /// Applies filter criteria from a FilterKey to the current filter model.
    /// IMPORTANT: Search criteria is stored in _filterKeySearch to avoid exposing PII in the UI.
    /// </summary>
    private void ApplyFilterCriteria(FilterCriteria criteria)
    {
        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            // Store in internal field, NOT in _filter.Search (which shows in the search box UI)
            _filterKeySearch = criteria.Search;
        }

        if (criteria.DateFrom.HasValue)
        {
            _filter.DateFrom = criteria.DateFrom.Value;
        }

        if (criteria.DateTo.HasValue)
        {
            _filter.DateTo = criteria.DateTo.Value;
        }

        if (criteria.ServerId.HasValue)
        {
            _filter.ServerId = criteria.ServerId.Value;
        }

        if (criteria.ConnectionTypes != null)
        {
            _filter.ShowAccepted = criteria.ConnectionTypes.ShowAccepted;
            _filter.ShowBanned = criteria.ConnectionTypes.ShowBanned;
            _filter.ShowWhitelist = criteria.ConnectionTypes.ShowWhitelist;
            _filter.ShowFull = criteria.ConnectionTypes.ShowFull;
            _filter.ShowPanic = criteria.ConnectionTypes.ShowPanic;
            _filter.ShowBabyJail = criteria.ConnectionTypes.ShowBabyJail;
            _filter.ShowIPChecks = criteria.ConnectionTypes.ShowIPChecks;
        }
    }

    /// <summary>
    /// Redacts an IP address based on PII settings
    /// </summary>
    private string RedactIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || !_shouldCensorPii)
            return ipAddress;

        // Try to determine if it's IPv4 or IPv6
        if (System.Net.IPAddress.TryParse(ipAddress, out var ip))
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                return PiiRedactor!.RedactIPv4(ipAddress);
            else
                return PiiRedactor!.RedactIPv6(ipAddress);
        }

        return ipAddress;
    }

    /// <summary>
    /// Redacts a HWID based on PII settings
    /// </summary>
    private string RedactHwid(string hwid)
    {
        if (string.IsNullOrWhiteSpace(hwid) || !_shouldCensorPii)
            return hwid;

        return PiiRedactor!.RedactHardwareId(hwid);
    }

    public void Dispose()
    {
        if (ClientPreferences != null)
        {
            ClientPreferences.OnChange -= OnPreferencesChanged;
        }
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
