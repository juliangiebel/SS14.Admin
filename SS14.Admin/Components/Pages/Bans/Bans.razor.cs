using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Components.Shared.Filter;
using SS14.Admin.Helpers;

namespace SS14.Admin.Components.Pages.Bans;

public partial class Bans
{
    [Inject]
    private PostgresServerDbContext? Context { get; set; }

    public QuickGrid<BanViewModel> Grid { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 13 };

    private IFilterModel _model = new BansFilterModel();

    // Cache of ban data.
    private IQueryable<BanViewModel> _bansQuery = Enumerable.Empty<BanViewModel>().AsQueryable();

    // Tracks confirmation state for each ban.
    private Dictionary<int, bool> _confirmations = new();

    protected override async Task OnInitializedAsync()
    {
        await Refresh();
    }

    private IQueryable<BanViewModel> GetBansQuery() =>
        from ban in Context!.Ban.AsNoTracking().Include(b => b.BanHits)
        join player in Context.Player.AsNoTracking() on ban.PlayerUserId equals player.UserId into playerJoin
        from p in playerJoin.DefaultIfEmpty()
        join admin in Context.Player.AsNoTracking() on ban.BanningAdmin equals admin.UserId into adminJoin
        from a in adminJoin.DefaultIfEmpty()
        orderby ban.BanTime descending  //sort by newest first by default
        select new BanViewModel
        {
            Id = ban.Id,
            PlayerUserId = ban.PlayerUserId.ToString(),
            PlayerName = p != null ? p.LastSeenUserName : "",
            IPAddress = ban.Address != null ? ban.Address.ToString() : "",
            Hwid = ban.HWId != null ? ban.HWId.ToImmutable().ToString() : null,
            Reason = ban.Reason,
            BanTime = ban.BanTime,
            ExpirationTime = ban.ExpirationTime,
            HitCount = ban.BanHits.Count,
            Admin = a != null ? a.LastSeenUserName : "",
            Active = !ban.ExpirationTime.HasValue || ban.ExpirationTime > DateTime.UtcNow
        };


    // Refresh the cache and update the UI.
    private async Task Refresh()
    {
        _bansQuery = GetBansQuery();
        _confirmations.Clear();
        await InvokeAsync(StateHasChanged);
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

    // When the user confirms, perform the action (unban if active; reban if not) and refresh.
    private async Task ConfirmAction(int banId, bool active)
    {
        _confirmations[banId] = false;
        await InvokeAsync(StateHasChanged);
        if (active)
        {
            await UnbanBan(banId);
        }
        else
        {
            await RebanBan(banId);
        }
        await Refresh();
    }

    // Placeholder method for unbanning.
    private async Task UnbanBan(int banId)
    {
        Console.WriteLine($"Unban action confirmed for ban ID {banId}");
        // TODO: Implement unban logic
        await Task.CompletedTask;
    }

    // Placeholder method for re-banning.
    private async Task RebanBan(int banId)
    {
        Console.WriteLine($"Reban action confirmed for ban ID {banId}");
        // TODO: Implement reban logic.
        await Task.CompletedTask;
    }

    public class BansFilterModel : IFilterModel
    {
        public string Test { get; set; } = "test";
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
