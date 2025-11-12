using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;
using SS14.Admin.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using SS14.Admin.Services;

namespace SS14.Admin.Components.Pages.Players;

public partial class Players : IDisposable
{
    [Inject]
    private IDbContextFactory<PostgresServerDbContext>? ContextFactory { get; set; }

    [Inject]
    private AuthenticationStateProvider? AuthenticationStateProvider { get; set; }

    [Inject]
    private ClientPreferencesService? ClientPreferences { get; set; }

    [Inject]
    private IPiiRedactor? PiiRedactor { get; set; }

    [SupplyParameterFromForm(FormName = "playerFilter")]
    public PlayerFilterModel _model { get; set; } = new();

    private ClaimsPrincipal? _user;
    private bool _shouldCensorPii;

    public QuickGrid<PlayerViewModel> Grid { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 13 };

    private List<PlayerViewModel> _playersList = new();

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider!.GetAuthenticationStateAsync();
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

    private async Task Refresh()
    {
        await using var context = await ContextFactory!.CreateDbContextAsync();
        _playersList = await GetPlayersQuery(context).ToListAsync();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Redacts an IP address based on PII settings
    /// </summary>
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

    private async Task OnFilterSubmit(EditContext context)
    {
        await Refresh();
    }

    private IQueryable<PlayerViewModel> GetPlayersQuery(PostgresServerDbContext context)
    {
        var query = context.Player.AsNoTracking().AsQueryable();

        // Apply search filter using the existing SearchHelper
        if (!string.IsNullOrWhiteSpace(_model.Search))
        {
            query = SearchHelper.SearchPlayers(query, _model.Search, _user!);
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
