using Content.Server.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Components.Pages.Bans
{
    public partial class CreateBan : ComponentBase
    {
        [Inject]
        public PostgresServerDbContext DbContext { get; set; } = default!;

        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;

        public List<BanTemplate> Templates { get; set; } = new();
        public bool IsDropdownOpen { get; set; }
        protected override async Task OnInitializedAsync()
        {
            await LoadTemplates();
        }

        private async Task LoadTemplates()
        {
            Templates = await DbContext.BanTemplate.ToListAsync();
        }

        private void ToggleDropdown() => IsDropdownOpen = !IsDropdownOpen;

        private void UseTemplate(int templateId)
        {
            NavigationManager.NavigateTo($"/bans/use-template/{templateId}");
        }
    }
}
