using Content.Server.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;
using Microsoft.AspNetCore.Components.Authorization;

namespace SS14.Admin.Components.Pages.Connections;

public partial class ConnectionHits
{
    [Inject]
    private IDbContextFactory<PostgresServerDbContext>? ContextFactory { get; set; }

    [Inject]
    private BanHelper? BanHelper { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    [Parameter]
    public int ConnectionId { get; set; }

    public QuickGrid<BanHitViewModel>? Grid { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 50 };

    private ConnectionViewModel? Connection { get; set; }

    private List<BanHitViewModel> _banHits = new();

    private string _searchText = "";

    private bool ShowPII { get; set; }

    private bool _loading = true;

    // Tracks confirmation state for each ban
    private Dictionary<int, bool> _confirmations = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Connection?.Id != ConnectionId)
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

        // Load connection information
        var connectionLog = await context.ConnectionLog
            .AsNoTracking()
            .Include(c => c.Server)
            .SingleOrDefaultAsync(c => c.Id == ConnectionId);

        if (connectionLog != null)
        {
            Connection = new ConnectionViewModel
            {
                Id = connectionLog.Id,
                UserName = connectionLog.UserName,
                UserId = connectionLog.UserId,
                Address = connectionLog.Address?.ToString() ?? "",
                HWId = connectionLog.HWId != null ? BanHelper.FormatHwid(connectionLog.HWId) ?? "" : "",
                Time = connectionLog.Time,
                ServerName = connectionLog.Server.Name,
                Denied = connectionLog.Denied
            };
        }

        if (Connection != null)
        {
            var now = DateTime.UtcNow;

            // Load bans that were hit by this connection
            var bansQuery = from hit in context.ServerBanHit.AsNoTracking()
                            join banJoin in BanHelper!.CreateServerBanJoin(context) on hit.BanId equals banJoin.Ban.Id
                            where hit.ConnectionId == ConnectionId
                            select new BanHitViewModel
                            {
                                Id = banJoin.Ban.Id,
                                PlayerUserId = banJoin.Ban.PlayerUserId != null ? banJoin.Ban.PlayerUserId.ToString() : "",
                                PlayerName = banJoin.Player != null ? banJoin.Player.LastSeenUserName : "",
                                IPAddress = banJoin.Ban.Address != null ? banJoin.Ban.Address.ToString() : "",
                                Hwid = banJoin.Ban.HWId != null ? banJoin.Ban.HWId.ToString() : "",
                                Reason = banJoin.Ban.Reason,
                                BanTime = banJoin.Ban.BanTime,
                                ExpirationTime = banJoin.Ban.ExpirationTime,
                                RoundId = banJoin.Ban.RoundId,
                                Admin = banJoin.Admin != null ? banJoin.Admin.LastSeenUserName : "",
                                Unban = banJoin.Ban.Unban,
                                Active = banJoin.Ban.Unban == null && (banJoin.Ban.ExpirationTime == null || banJoin.Ban.ExpirationTime > now),
                                HitCount = banJoin.Ban.BanHits.Count
                            };

            _banHits = await bansQuery.ToListAsync();
        }

        _loading = false;
        await InvokeAsync(StateHasChanged);
    }

    private IQueryable<BanHitViewModel> GetFilteredBans()
    {
        var query = _banHits.AsQueryable();

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var searchLower = _searchText.ToLower();
            query = query.Where(b =>
                (b.PlayerName != null && b.PlayerName.ToLower().Contains(searchLower)) ||
                b.PlayerUserId.ToLower().Contains(searchLower) ||
                (b.Reason != null && b.Reason.ToLower().Contains(searchLower)) ||
                (b.Admin != null && b.Admin.ToLower().Contains(searchLower))
            );
        }

        return query;
    }

    private async Task RefreshGrid()
    {
        await LoadData();
    }

    // When a user clicks the unban button, show confirmation
    private async Task ShowConfirmation(int banId)
    {
        _confirmations[banId] = true;
        await InvokeAsync(StateHasChanged);
        await Task.Delay(3000);
        _confirmations[banId] = false;
        await InvokeAsync(StateHasChanged);
    }

    // When the user confirms, unban the ban and refresh
    private async Task ConfirmUnban(int banId)
    {
        _confirmations[banId] = false;
        await InvokeAsync(StateHasChanged);

        await UnbanBan(banId);
        await LoadData();
    }

    // Unban a ban by creating an Unban entity
    private async Task UnbanBan(int banId)
    {
        await using var context = await ContextFactory!.CreateDbContextAsync();

        var ban = await context.Ban
            .Include(b => b.Unban)
            .SingleOrDefaultAsync(b => b.Id == banId);

        if (ban == null)
        {
            return;
        }

        // Check if already unbanned
        if (ban.Unban != null)
        {
            return;
        }

        // Get the current admin's user ID
        var authState = await AuthenticationState!;
        var user = authState.User;
        var adminId = user.Claims.GetUserId();

        // Create the unban record
        ban.Unban = new ServerUnban
        {
            Ban = ban,
            UnbanningAdmin = adminId,
            UnbanTime = DateTime.UtcNow
        };

        await context.SaveChangesAsync();
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
        public ConnectionDenyReason? Denied { get; set; }
    }

    public class BanHitViewModel
    {
        public int Id { get; set; }
        public string PlayerUserId { get; set; } = "";
        public string PlayerName { get; set; } = "";
        public string IPAddress { get; set; } = "";
        public string Hwid { get; set; } = "";
        public string? Reason { get; set; }
        public DateTime BanTime { get; set; }
        public DateTime? ExpirationTime { get; set; }
        public int? RoundId { get; set; }
        public string? Admin { get; set; }
        public ServerUnban? Unban { get; set; }
        public bool Active { get; set; }
        public int HitCount { get; set; }
    }
}
