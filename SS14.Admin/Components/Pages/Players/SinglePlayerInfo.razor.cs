using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;
using System.Security.Claims;
using System.Text.Json;

namespace SS14.Admin.Components.Pages.Players
{
    public partial class SinglePlayerInfo : ComponentBase
    {
    [Inject] private IDbContextFactory<PostgresServerDbContext>? ContextFactory { get; set; }
    [Inject] private BanHelper? BanHelper { get; set; }
    [Inject] private AuthenticationStateProvider? AuthStateProvider { get; set; }
    [Inject] private IHttpClientFactory? HttpClientFactory { get; set; }
    [Parameter] public Guid userId { get; set; }

        public PlayerViewModel? PlayerModel { get; set; }
        public bool Whitelisted { get; set; }
        public List<PlayTimeViewModel> PlayTimes { get; set; } = new();
        public List<ProfileViewModel> Profiles { get; set; } = new();
        public List<RemarkViewModel> Remarks { get; set; } = new();
        public List<BanViewModel> Bans { get; set; } = new();
        public List<RoleBanViewModel> RoleBans { get; set; } = new();
        public DateTime? AccountCreationDate { get; set; }
        public bool HasActiveBan => Bans.Any(b => b.IsActive);

        private bool _isLoading = true;
        private ClaimsPrincipal? _user;

        private bool _showWhitelistDialog;
        private string _whitelistDialogTitle = string.Empty;
        private string _whitelistDialogMessage = string.Empty;
        private string _whitelistConfirmText = string.Empty;
        private string _whitelistButtonClass = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            await LoadPlayerDataAsync();
        }

        private async Task LoadPlayerDataAsync()
        {
            _isLoading = true;

            // Get the current user for PII checks
            var authState = await AuthStateProvider!.GetAuthenticationStateAsync();
            _user = authState.User;

            await using var context = await ContextFactory!.CreateDbContextAsync();

            var player = await context.Player.AsNoTracking()
                .SingleOrDefaultAsync(p => p.UserId == userId);

            if (player == null)
            {
                _isLoading = false;
                return;
            }

            PlayerModel = new PlayerViewModel
            {
                Id = player.Id,
                LastSeenUsername = player.LastSeenUserName,
                Guid = player.UserId.ToString(),
                LastSeen = player.LastSeenTime,
                FirstSeen = player.FirstSeenTime,
                LastSeenIPAddress = player.LastSeenAddress.ToString(),
                LastSeenHwid = player.LastSeenHWId != null ? BanHelper.FormatHwid(player.LastSeenHWId) : ""
            };

            // Fetch account creation date from SS14 auth API
            await FetchAccountCreationDateAsync(userId);

            // Load whitelist status
            Whitelisted = await WhitelistHelper.IsWhitelistedAsync(context, userId);

            // Load play times
            PlayTimes = await context.PlayTime
                .Where(pt => pt.PlayerId == userId)
                .Select(pt => new PlayTimeViewModel
                {
                    Tracker = pt.Tracker,
                    TimeSpent = pt.TimeSpent
                })
                .ToListAsync();

            // Load character profiles
            Profiles = await context.Profile
                .AsNoTracking()
                .Where(p => p.Preference.UserId == userId)
                .OrderBy(p => p.Slot)
                .Include(p => p.Jobs)
                .Select(p => new ProfileViewModel
                {
                    CharacterName = p.CharacterName,
                    Age = p.Age,
                    Sex = p.Sex,
                    Gender = p.Gender,
                    Species = p.Species,
                    FavoriteJob = p.Jobs.Where(j => j.Priority == DbJobPriority.High)
                        .Select(j => j.JobName).FirstOrDefault() ?? "none"
                })
                .ToListAsync();

            // Load remarks (notes, messages, watchlist)
            var notes = await context.AdminNotes
                .Where(n => n.PlayerUserId == userId && !n.Deleted && (n.ExpirationTime == null || n.ExpirationTime > DateTime.UtcNow))
                .Include(n => n.CreatedBy)
                .Include(n => n.LastEditedBy)
                .Select(n => new RemarkViewModel
                {
                    Type = "Note",
                    Message = n.Message,
                    RoundId = n.RoundId,
                    Severity = n.Severity,
                    IsSecret = n.Secret,
                    PlaytimeAtNote = n.PlaytimeAtNote,
                    ExpirationTime = n.ExpirationTime,
                    CreatedAt = n.CreatedAt,
                    CreatedByUsername = n.CreatedBy != null ? n.CreatedBy.LastSeenUserName : "unknown",
                    LastEditedAt = n.LastEditedAt,
                    LastEditedByUsername = n.LastEditedBy != null ? n.LastEditedBy.LastSeenUserName : "unknown"
                })
                .ToListAsync();

            var messages = await context.AdminMessages
                .Where(m => m.PlayerUserId == userId && !m.Deleted && (m.ExpirationTime == null || m.ExpirationTime > DateTime.UtcNow))
                .Include(m => m.CreatedBy)
                .Include(m => m.LastEditedBy)
                .Select(m => new RemarkViewModel
                {
                    Type = "Message",
                    Message = m.Message,
                    RoundId = m.RoundId,
                    Seen = m.Seen,
                    PlaytimeAtNote = m.PlaytimeAtNote,
                    ExpirationTime = m.ExpirationTime,
                    CreatedAt = m.CreatedAt,
                    CreatedByUsername = m.CreatedBy != null ? m.CreatedBy.LastSeenUserName : "unknown",
                    LastEditedAt = m.LastEditedAt,
                    LastEditedByUsername = m.LastEditedBy != null ? m.LastEditedBy.LastSeenUserName : "unknown"
                })
                .ToListAsync();

            var watchlist = await context.AdminWatchlists
                .Where(w => w.PlayerUserId == userId && !w.Deleted && (w.ExpirationTime == null || w.ExpirationTime > DateTime.UtcNow))
                .Include(w => w.CreatedBy)
                .Include(w => w.LastEditedBy)
                .Select(w => new RemarkViewModel
                {
                    Type = "Watchlist",
                    Message = w.Message,
                    RoundId = w.RoundId,
                    PlaytimeAtNote = w.PlaytimeAtNote,
                    ExpirationTime = w.ExpirationTime,
                    CreatedAt = w.CreatedAt,
                    CreatedByUsername = w.CreatedBy != null ? w.CreatedBy.LastSeenUserName : "unknown",
                    LastEditedAt = w.LastEditedAt,
                    LastEditedByUsername = w.LastEditedBy != null ? w.LastEditedBy.LastSeenUserName : "unknown"
                })
                .ToListAsync();

            Remarks = notes.Concat(messages).Concat(watchlist)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            // Load bans
            var serverBansQuery = BanHelper!.CreateServerBanJoin(context).AsNoTracking();
            var serverBans = SearchHelper.SearchServerBans(serverBansQuery, userId.ToString(), _user!);

            Bans = await serverBans
                .Select(b => new BanViewModel
                {
                    Id = b.Ban.Id,
                    Reason = b.Ban.Reason,
                    BanTime = b.Ban.BanTime,
                    ExpirationTime = b.Ban.ExpirationTime,
                    Address = b.Ban.Address != null ? b.Ban.Address.ToString() : null,
                    Hwid = b.Ban.HWId != null ? BanHelper.FormatHwid(b.Ban.HWId) : null,
                    AdminName = b.Admin != null ? b.Admin.LastSeenUserName : "System",
                    UnbanTime = b.Ban.Unban != null ? b.Ban.Unban.UnbanTime : null,
                    UnbanAdminName = b.UnbanAdmin != null ? b.UnbanAdmin.LastSeenUserName : null,
                    IsActive = BanHelper.IsBanActive(b.Ban)
                })
                .OrderByDescending(b => b.BanTime)
                .ToListAsync();

            // Load role bans
            var roleBansQuery = BanHelper!.CreateRoleBanJoin(context).AsNoTracking();
            var roleBans = SearchHelper.SearchRoleBans(roleBansQuery, userId.ToString(), _user!);

            RoleBans = await roleBans
                .Select(b => new RoleBanViewModel
                {
                    Id = b.Ban.Id,
                    Role = b.Ban.RoleId,
                    Reason = b.Ban.Reason,
                    BanTime = b.Ban.BanTime,
                    ExpirationTime = b.Ban.ExpirationTime,
                    Address = b.Ban.Address != null ? b.Ban.Address.ToString() : null,
                    Hwid = b.Ban.HWId != null ? BanHelper.FormatHwid(b.Ban.HWId) : null,
                    AdminName = b.Admin != null ? b.Admin.LastSeenUserName : "System",
                    UnbanTime = b.Ban.Unban != null ? b.Ban.Unban.UnbanTime : null,
                    UnbanAdminName = b.UnbanAdmin != null ? b.UnbanAdmin.LastSeenUserName : null,
                    IsActive = BanHelper.IsBanActive(b.Ban)
                })
                .OrderByDescending(b => b.BanTime)
                .ToListAsync();

            _isLoading = false;
        }

        private void ToggleWhitelistAsync()
        {
            if (Whitelisted)
            {
                _whitelistDialogTitle = "Remove from Whitelist";
                _whitelistDialogMessage = $"Are you sure you want to remove '{PlayerModel?.LastSeenUsername}' ({userId}) from the whitelist?";
                _whitelistConfirmText = "Remove";
                _whitelistButtonClass = "bg-red-600 text-white hover:bg-red-700";
            }
            else
            {
                _whitelistDialogTitle = "Add to Whitelist";
                _whitelistDialogMessage = $"Are you sure you want to add '{PlayerModel?.LastSeenUsername}' ({userId}) to the whitelist?";
                _whitelistConfirmText = "Add";
                _whitelistButtonClass = "bg-green-600 text-white hover:bg-green-700";
            }
            _showWhitelistDialog = true;
        }

        private async Task ConfirmToggleWhitelistAsync()
        {
            _showWhitelistDialog = false;

            try
            {
                await using var context = await ContextFactory!.CreateDbContextAsync();

                if (Whitelisted)
                {
                    await WhitelistHelper.RemoveWhitelistAsync(context, userId);
                }
                else
                {
                    await WhitelistHelper.AddWhitelistAsync(context, userId);
                }

                Whitelisted = !Whitelisted;
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception)
            {
                // Handle error silently or show a message
            }
        }

        private void CancelToggleWhitelist()
        {
            _showWhitelistDialog = false;
        }

        private async Task FetchAccountCreationDateAsync(Guid userId)
        {
            try
            {
                var httpClient = HttpClientFactory!.CreateClient();
                var response = await httpClient.GetAsync($"https://auth.spacestation14.com/api/query/userid?userId={userId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var authData = JsonSerializer.Deserialize<AuthApiResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (authData != null && authData.CreatedTime.HasValue)
                    {
                        AccountCreationDate = authData.CreatedTime.Value;
                    }
                }
            }
            catch (Exception)
            {
                // Silently fail if we can't fetch the account creation date
                // this should probably return some error
                AccountCreationDate = null;
            }
        }

        private class AuthApiResponse
        {
            public string? UserName { get; set; }
            public Guid? UserId { get; set; }
            public string? PatronTier { get; set; }
            public DateTime? CreatedTime { get; set; }
        }

        public class PlayerViewModel
        {
            public int Id { get; set; }
            public string LastSeenUsername { get; set; } = "";
            public string Guid { get; set; } = "";
            public DateTime LastSeen { get; set; }
            public DateTime FirstSeen { get; set; }
            public string LastSeenIPAddress { get; set; } = "";
            public string LastSeenHwid { get; set; } = "";
        }

        public class PlayTimeViewModel
        {
            public string Tracker { get; set; } = "";
            public TimeSpan TimeSpent { get; set; }
        }

        public class ProfileViewModel
        {
            public string CharacterName { get; set; } = "";
            public int Age { get; set; }
            public string Sex { get; set; } = "";
            public string Gender { get; set; } = "";
            public string Species { get; set; } = "";
            public string FavoriteJob { get; set; } = "";
        }

        public class RemarkViewModel
        {
            public string Type { get; set; } = "";
            public string Message { get; set; } = "";
            public int? RoundId { get; set; }
            public NoteSeverity? Severity { get; set; }
            public bool? IsSecret { get; set; }
            public bool? Seen { get; set; }
            public TimeSpan PlaytimeAtNote { get; set; }
            public DateTime? ExpirationTime { get; set; }
            public DateTime CreatedAt { get; set; }
            public string CreatedByUsername { get; set; } = "";
            public DateTime? LastEditedAt { get; set; }
            public string LastEditedByUsername { get; set; } = "";
        }

        public class BanViewModel
        {
            public int Id { get; set; }
            public string Reason { get; set; } = "";
            public DateTime BanTime { get; set; }
            public DateTime? ExpirationTime { get; set; }
            public string? Address { get; set; }
            public string? Hwid { get; set; }
            public string AdminName { get; set; } = "";
            public DateTime? UnbanTime { get; set; }
            public string? UnbanAdminName { get; set; }
            public bool IsActive { get; set; }
        }

        public class RoleBanViewModel
        {
            public int Id { get; set; }
            public string Role { get; set; } = "";
            public string Reason { get; set; } = "";
            public DateTime BanTime { get; set; }
            public DateTime? ExpirationTime { get; set; }
            public string? Address { get; set; }
            public string? Hwid { get; set; }
            public string AdminName { get; set; } = "";
            public DateTime? UnbanTime { get; set; }
            public string? UnbanAdminName { get; set; }
            public bool IsActive { get; set; }
        }
    }
}
