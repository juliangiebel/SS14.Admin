using Content.Server.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Components.Forms;

namespace SS14.Admin.Components.Pages.Bans
{
    public partial class CreateBan : ComponentBase
    {
        [Inject]
        public PostgresServerDbContext DbContext { get; set; } = default!;

        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;

        public List<BanTemplate> Templates { get; set; } = new();

        private CreateBanForm.CreateBanModel _banModel = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadTemplates();
        }

        private async Task LoadTemplates()
        {
            Templates = await DbContext.BanTemplate.ToListAsync();
        }

        private void UseTemplate(BanTemplate template)
        {
            _banModel.Reason = template.Reason;
            _banModel.LengthMinutes = (int)template.Length.TotalMinutes;
            _banModel.Severity = template.Severity;
            _banModel.Hidden = template.Hidden;
            _banModel.ExemptFlags = template.ExemptFlags;

            StateHasChanged();
        }

        private string GetDurationDisplay(TimeSpan duration)
        {
            if (duration.TotalMinutes == 0)
                return "Permanent";

            if (duration.TotalDays >= 30)
                return $"{(int)(duration.TotalDays / 30)}M";
            if (duration.TotalDays >= 7)
                return $"{(int)(duration.TotalDays / 7)}w";
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays}d";
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}h";

            return $"{(int)duration.TotalMinutes}m";
        }
    }
}
