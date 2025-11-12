using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Authorization;
using SS14.Admin.Helpers;
using System.Security.Claims;
using SS14.Admin.Services;

namespace SS14.Admin.Components.Pages.Bans;

public partial class Bans : IDisposable
{
    [Inject]
    private IDbContextFactory<PostgresServerDbContext>? ContextFactory { get; set; }

    [Inject]
    private AuthenticationStateProvider? AuthStateProvider { get; set; }

    [Inject]
    private ClientPreferencesService? ClientPreferences { get; set; }

    [Inject]
    private IPiiRedactor? PiiRedactor { get; set; }

    private ClaimsPrincipal? _user;
    private bool _shouldCensorPii;

    [SupplyParameterFromForm(FormName = "banFilter")]
    public BansFilterModel _model { get; set; } = new();

    public QuickGrid<BanViewModel> Grid { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 13 };

    // Cache of ban data.
    private List<BanViewModel> _bansList = new();

    // Tracks confirmation state for each ban.
    private Dictionary<int, bool> _confirmations = new();

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider!.GetAuthenticationStateAsync();
        _user = authState.User;

        // Initially censor PII until we can load client preferences
        var hasPiiPermission = _user.IsInRole(Constants.PIIRole);
        _shouldCensorPii = !hasPiiPermission;

        ClientPreferences!.OnChange += OnPreferencesChanged;

        await Refresh();
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

    private void OnPreferencesChanged(ClientPreferencesService.ClientPreferences preferences)
    {
        var hasPiiPermission = _user?.IsInRole(Constants.PIIRole) ?? false;
        _shouldCensorPii = !hasPiiPermission || preferences.censorPii;
        InvokeAsync(StateHasChanged);
    }

    private string RedactIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || !_shouldCensorPii)
            return ipAddress;

        if (System.Net.IPAddress.TryParse(ipAddress, out var ip))
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                return PiiRedactor!.RedactIPv4(ipAddress);
            else
                return PiiRedactor!.RedactIPv6(ipAddress);
        }
        return ipAddress;
    }

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

    private async Task<List<(ServerBan ban, Player? player, Player? admin)>> GetBansQueryEntities(PostgresServerDbContext context)
    {
        var now = DateTime.UtcNow;

        // Start with base ban query with joins for search capability, including Unban
        var baseQuery = from ban in context.Ban.AsNoTracking().Include(b => b.BanHits).Include(b => b.Unban)
                        join player in context.Player.AsNoTracking() on ban.PlayerUserId equals player.UserId into playerJoin
                        from p in playerJoin.DefaultIfEmpty()
                        join admin in context.Player.AsNoTracking() on ban.BanningAdmin equals admin.UserId into adminJoin
                        from a in adminJoin.DefaultIfEmpty()
                        select new { ban, p, a };

        // Apply search filter at database level before other filters
        if (!string.IsNullOrWhiteSpace(_model.Search))
        {
            var search = _model.Search.ToLower();
            baseQuery = baseQuery.Where(x =>
                (x.p != null && x.p.LastSeenUserName.ToLower().Contains(search)) ||
                EF.Functions.Like(x.ban.PlayerUserId.ToString().ToLower(), $"%{search}%") ||
                (x.ban.Reason != null && x.ban.Reason.ToLower().Contains(search)) ||
                (x.a != null && x.a.LastSeenUserName.ToLower().Contains(search))
            );
        }

        // Apply date filters at the database level
        if (_model.DateFrom.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.ban.BanTime >= _model.DateFrom.Value);
        }

        if (_model.DateTo.HasValue)
        {
            var dateTo = _model.DateTo.Value.AddDays(1);
            baseQuery = baseQuery.Where(x => x.ban.BanTime < dateTo);
        }

        if (_model.ExpiresFrom.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.ban.ExpirationTime >= _model.ExpiresFrom.Value);
        }

        if (_model.ExpiresTo.HasValue)
        {
            var expiresTo = _model.ExpiresTo.Value.AddDays(1);
            baseQuery = baseQuery.Where(x => x.ban.ExpirationTime < expiresTo);
        }

        // Apply status filters at the database level
        // A ban is active if it has no Unban record AND (no expiration OR expiration in future)
        if (_model.ShowActive && !_model.ShowExpired)
        {
            // Only active: no unban AND (no expiration or expiration in future)
            baseQuery = baseQuery.Where(x => x.ban.Unban == null && (!x.ban.ExpirationTime.HasValue || x.ban.ExpirationTime > now));
        }
        else if (!_model.ShowActive && _model.ShowExpired)
        {
            // Only expired/unbanned: has unban OR expiration time in the past
            baseQuery = baseQuery.Where(x => x.ban.Unban != null || (x.ban.ExpirationTime.HasValue && x.ban.ExpirationTime <= now));
        }
        else if (!_model.ShowActive && !_model.ShowExpired)
        {
            // Neither selected - return empty
            return new List<(ServerBan, Player?, Player?)>();
        }
        // If both are true, show all (no filter needed)

        var results = await baseQuery.OrderByDescending(x => x.ban.BanTime).ToListAsync();
        return results.Select(x => (x.ban, x.p, x.a)).ToList();
    }


    // Refresh the cache and update the UI.
    private async Task Refresh()
    {
        await using var context = await ContextFactory!.CreateDbContextAsync();
        var entities = await GetBansQueryEntities(context);

        var now = DateTime.UtcNow;

        // Map entities to view models with proper HWID formatting
        _bansList = entities.Select(x => new BanViewModel
        {
            Id = x.ban.Id,
            PlayerUserId = x.ban.PlayerUserId.ToString(),
            PlayerName = x.player != null ? x.player.LastSeenUserName : "",
            IPAddress = x.ban.Address != null ? x.ban.Address.ToString() : "",
            Hwid = x.ban.HWId != null ? x.ban.HWId.ToImmutable().ToString() : "",
            Reason = x.ban.Reason,
            BanTime = x.ban.BanTime,
            ExpirationTime = x.ban.ExpirationTime,
            HitCount = x.ban.BanHits.Count,
            Admin = x.admin != null ? x.admin.LastSeenUserName : "",
            Active = x.ban.Unban == null && (!x.ban.ExpirationTime.HasValue || x.ban.ExpirationTime > now)
        }).ToList();

        _confirmations.Clear();
        await InvokeAsync(StateHasChanged);
    }

    // Handle filter submission
    private async Task OnFilterSubmit(EditContext context)
    {
        await Refresh();
    }

    // When a user clicks the action button, show confirmation.
    private async Task ShowConfirmation(int banId, bool active)
    {
        _confirmations[banId] = true;
        await InvokeAsync(StateHasChanged);
        await Task.Delay(3000);
        _confirmations[banId] = false;
        await InvokeAsync(StateHasChanged);
    }

    // When the user confirms, unban the ban and refresh
    private async Task ConfirmAction(int banId, bool active)
    {
        _confirmations[banId] = false;
        await InvokeAsync(StateHasChanged);

        // Only active bans can be unbanned
        if (active)
        {
            await UnbanBan(banId);
        }

        await Refresh();
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
        var authState = await AuthStateProvider!.GetAuthenticationStateAsync();
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

    public class BanViewModel
    {
        public int Id { get; set; }
        public string Reason { get; set; } = "";
        public DateTime BanTime { get; set; }
        public int? Round { get; set; }
        public DateTime? ExpirationTime { get; set; }
        public int HitCount { get; set; }
        public string Admin { get; set; } = "";
        public string PlayerName { get; set; } = "";
        public bool Active { get; set; }

        //PII
        public string IPAddress { get; set; } = "";
        public string Hwid { get; set; } = "";
        public string PlayerUserId { get; set; }
    }
}
