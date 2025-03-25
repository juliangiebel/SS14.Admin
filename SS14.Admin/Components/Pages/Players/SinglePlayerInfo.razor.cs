using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SS14.Admin.Components.Pages.Players
{
    public partial class SinglePlayerInfo : ComponentBase
    {
        [Inject] private PostgresServerDbContext? Context { get; set; }
        [Parameter] public Guid userId { get; set; }
        public PlayerViewModel? PlayerModel { get; set; }

        protected override async Task OnInitializedAsync()
        {
            PlayerModel = await GetPlayerViewModelAsync(userId);
        }

        private async Task<PlayerViewModel?> GetPlayerViewModelAsync(Guid userId)
        {
            return await (
                from player in Context.Player.AsNoTracking()
                where player.UserId == userId
                select new PlayerViewModel
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
                }
            ).SingleOrDefaultAsync();
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
}
