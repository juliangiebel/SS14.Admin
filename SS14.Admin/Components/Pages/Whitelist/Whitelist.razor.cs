using Content.Server.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;
using SS14.Admin.Models;

namespace SS14.Admin.Components.Pages.Whitelist;

public partial class Whitelist
{
    [Inject] private PostgresServerDbContext Context { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromForm(FormName = "WhitelistFilter")]
    private WhitelistFilterModel _filterModel { get; set; } = new();

    private QuickGrid<WhitelistViewModel> Grid { get; set; } = null!;
    private PaginationState _pagination = new() { ItemsPerPage = 20 };
    private List<WhitelistViewModel> _whitelistData = new();
    private IQueryable<WhitelistViewModel> _whitelistQuery => ApplyFilters(_whitelistData.AsQueryable());

    private string NewGuid { get; set; } = string.Empty;
    private string Message { get; set; } = string.Empty;
    private bool IsError { get; set; }
    private bool IsProcessing { get; set; }
    private bool IsValidGuid => Guid.TryParse(NewGuid, out _);

    private bool _showConfirmDialog;
    private string _confirmMessage = string.Empty;
    private Guid _userIdToRemove;

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var joins = WhitelistHelper.MakeWhitelistJoin(Context)
            .OrderByDescending(j => j.Player != null ? j.Player.LastSeenTime : DateTime.MinValue);

        var joinsList = await EntityFrameworkQueryableExtensions.ToListAsync(joins);

        _whitelistData = joinsList.Select(j => new WhitelistViewModel
        {
            UserId = j.Whitelist.UserId,
            LastSeenUsername = j.Player?.LastSeenUserName ?? "Unknown",
            LastSeenTime = j.Player?.LastSeenTime
        }).ToList();

        await InvokeAsync(StateHasChanged);
    }

    private IQueryable<WhitelistViewModel> ApplyFilters(IQueryable<WhitelistViewModel> query)
    {
        if (!string.IsNullOrWhiteSpace(_filterModel.Search))
        {
            var searchLower = _filterModel.Search.ToLower();
            query = query.Where(w =>
                w.UserId.ToString().ToLower().Contains(searchLower) ||
                w.LastSeenUsername.ToLower().Contains(searchLower));
        }

        return query;
    }

    private async Task OnFilterSubmit(EditContext context)
    {
        await RefreshAsync();
    }


    private async Task AddWhitelistAsync()
    {
        if (!IsValidGuid || IsProcessing)
            return;

        IsProcessing = true;
        Message = string.Empty;

        try
        {
            var guid = Guid.Parse(NewGuid);
            var success = await WhitelistHelper.AddWhitelistAsync(Context, guid);

            if (success)
            {
                Message = $"Successfully added {guid} to whitelist";
                IsError = false;
                NewGuid = string.Empty;
                await RefreshAsync();
            }
            else
            {
                Message = $"User {guid} is already whitelisted";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            Message = $"Error adding to whitelist: {ex.Message}";
            IsError = true;
        }
        finally
        {
            IsProcessing = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void RemoveWhitelistAsync(Guid userId)
    {
        var username = _whitelistData.FirstOrDefault(w => w.UserId == userId)?.LastSeenUsername ?? "Unknown";
        _userIdToRemove = userId;
        _confirmMessage = $"Are you sure you want to remove '{username}' ({userId}) from the whitelist?";
        _showConfirmDialog = true;
    }

    private async Task ConfirmRemoveWhitelistAsync()
    {
        if (IsProcessing)
            return;

        _showConfirmDialog = false;
        IsProcessing = true;
        Message = string.Empty;

        try
        {
            var success = await WhitelistHelper.RemoveWhitelistAsync(Context, _userIdToRemove);

            if (success)
            {
                Message = $"Successfully removed {_userIdToRemove} from whitelist";
                IsError = false;
                await RefreshAsync();
            }
            else
            {
                Message = $"User {_userIdToRemove} was not found in whitelist";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            Message = $"Error removing from whitelist: {ex.Message}";
            IsError = true;
        }
        finally
        {
            IsProcessing = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void CancelRemoveWhitelist()
    {
        _showConfirmDialog = false;
        _userIdToRemove = Guid.Empty;
    }

    public class WhitelistViewModel
    {
        public Guid UserId { get; set; }
        public string LastSeenUsername { get; set; } = "";
        public DateTime? LastSeenTime { get; set; }
    }
}
