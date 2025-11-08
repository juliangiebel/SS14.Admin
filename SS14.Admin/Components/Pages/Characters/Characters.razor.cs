using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.EntityFrameworkCore;
using Content.Server.Database;
using SS14.Admin.Models;

namespace SS14.Admin.Components.Pages.Characters;

public partial class Characters
{
    [Inject] private PostgresServerDbContext Context { get; set; } = null!;

    [SupplyParameterFromForm(FormName = "CharacterFilter")]
    private CharactersFilterModel _filterModel { get; set; } = new();

    private QuickGrid<CharacterViewModel> Grid { get; set; } = null!;
    private PaginationState _pagination = new() { ItemsPerPage = 20 };
    private List<CharacterViewModel> _characterData = new();
    private IQueryable<CharacterViewModel> _characterQuery => ApplyFilters(_characterData.AsQueryable());

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var query = from profile in Context.Profile.AsNoTracking()
                    join pref in Context.Preference.AsNoTracking() on profile.PreferenceId equals pref.Id
                    join player in Context.Player.AsNoTracking() on pref.UserId equals player.UserId into playerJoin
                    from p in playerJoin.DefaultIfEmpty()
                    select new CharacterViewModel
                    {
                        CharacterName = profile.CharacterName,
                        PlayerUserId = pref.UserId,
                        PlayerName = p != null ? p.LastSeenUserName : "Unknown",
                        Slot = profile.Slot,
                        Species = profile.Species,
                        Age = profile.Age,
                        Sex = profile.Sex.ToString(),
                        Gender = profile.Gender.ToString()
                    };

        _characterData = await query.OrderBy(c => c.PlayerName).ThenBy(c => c.Slot).ToListAsync();
        await InvokeAsync(StateHasChanged);
    }

    private IQueryable<CharacterViewModel> ApplyFilters(IQueryable<CharacterViewModel> query)
    {
        if (!string.IsNullOrWhiteSpace(_filterModel.Search))
        {
            var searchLower = _filterModel.Search.ToLower();
            query = query.Where(c =>
                c.CharacterName.ToLower().Contains(searchLower) ||
                c.PlayerName.ToLower().Contains(searchLower) ||
                c.PlayerUserId.ToString().ToLower().Contains(searchLower));
        }

        return query;
    }

    private async Task OnFilterSubmit(EditContext context)
    {
        await RefreshAsync();
    }

    public class CharacterViewModel
    {
        public string CharacterName { get; set; } = "";
        public Guid PlayerUserId { get; set; }
        public string PlayerName { get; set; } = "";
        public int Slot { get; set; }
        public string Species { get; set; } = "";
        public int Age { get; set; }
        public string Sex { get; set; } = "";
        public string Gender { get; set; } = "";
    }
}
