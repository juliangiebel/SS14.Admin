using Content.Server.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;
using Microsoft.AspNetCore.Components.Authorization;

namespace SS14.Admin.Components.Pages.Bans;

public partial class BanHits
{
    [Inject]
    private IDbContextFactory<PostgresServerDbContext>? ContextFactory { get; set; }

    [Inject]
    private BanHelper? BanHelper { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    [Parameter]
    public int BanId { get; set; }

    public QuickGrid<ConnectionHitViewModel>? Grid { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 50 };

    private BanViewModel? Ban { get; set; }

    private List<ConnectionHitViewModel> _connectionHits = new();

    private string _searchText = "";

    private bool ShowPII { get; set; }

    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Ban?.Id != BanId)
        {
            await LoadData();
        }
    }

    private async Task LoadData()
    {
        _loading = true;

        // Check if user has PII role
        var authState = await AuthenticationState!;
        ShowPII = authState.User.IsInRole(Constants.PIIRole);

        await using var context = await ContextFactory!.CreateDbContextAsync();

        // Load ban information using BanHelper
        var banEntry = await BanHelper!.CreateServerBanJoin(context)
            .SingleOrDefaultAsync(b => b.Ban.Id == BanId);

        if (banEntry != null)
        {
            Ban = new BanViewModel
            {
                Id = banEntry.Ban.Id,
                PlayerUserId = banEntry.Ban.PlayerUserId?.ToString() ?? "",
                PlayerName = banEntry.Player?.LastSeenUserName,
                IPAddress = banEntry.Ban.Address != null ? banEntry.Ban.Address.ToString() : "",
                Hwid = BanHelper.FormatHwid(banEntry.Ban.HWId) ?? "",
                Reason = banEntry.Ban.Reason,
                BanTime = banEntry.Ban.BanTime,
                ExpirationTime = banEntry.Ban.ExpirationTime,
                UnbanTime = banEntry.Ban.Unban?.UnbanTime,
                Admin = banEntry.Admin?.LastSeenUserName,
                UnbanAdmin = banEntry.UnbanAdmin?.LastSeenUserName
            };
        }

        if (Ban != null)
        {
            // Load connection hits
            var hitsQuery = from hit in context.ServerBanHit.AsNoTracking()
                            join connection in context.ConnectionLog.AsNoTracking().Include(c => c.Server)
                                on hit.ConnectionId equals connection.Id
                            where hit.BanId == BanId
                            orderby connection.Time descending
                            select connection;

            var connectionEntities = await hitsQuery.ToListAsync();

            // Map to view models with proper HWID formatting
            _connectionHits = connectionEntities.Select(connection => new ConnectionHitViewModel
            {
                Id = connection.Id,
                UserName = connection.UserName,
                UserId = connection.UserId,
                Address = connection.Address != null ? connection.Address.ToString() : "",
                HWId = BanHelper.FormatHwid(connection.HWId) ?? "",
                Time = connection.Time,
                ServerName = connection.Server.Name,
                ServerId = connection.ServerId,
                Denied = connection.Denied
            }).ToList();
        }

        _loading = false;
        await InvokeAsync(StateHasChanged);
    }

    private IQueryable<ConnectionHitViewModel> GetFilteredConnections()
    {
        var query = _connectionHits.AsQueryable();

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var searchLower = _searchText.ToLower();
            query = query.Where(c =>
                c.UserName.ToLower().Contains(searchLower) ||
                c.UserId.ToString().ToLower().Contains(searchLower)
            );
        }

        return query;
    }

    private async Task RefreshGrid()
    {
        await LoadData();
    }

    public class BanViewModel
    {
        public int Id { get; set; }
        public string PlayerUserId { get; set; } = "";
        public string? PlayerName { get; set; }
        public string IPAddress { get; set; } = "";
        public string Hwid { get; set; } = "";
        public string? Reason { get; set; }
        public DateTime BanTime { get; set; }
        public DateTime? ExpirationTime { get; set; }
        public DateTime? UnbanTime { get; set; }
        public string? Admin { get; set; }
        public string? UnbanAdmin { get; set; }
    }

    public class ConnectionHitViewModel
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
    }
}
