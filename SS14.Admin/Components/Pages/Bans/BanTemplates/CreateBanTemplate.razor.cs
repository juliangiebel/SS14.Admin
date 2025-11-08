using System.ComponentModel.DataAnnotations;
using Content.Server.Database;
using Microsoft.AspNetCore.Components;

namespace SS14.Admin.Components.Pages.Bans.BanTemplates
{
    public partial class CreateBanTemplate : ComponentBase
    {
        [Inject]
        protected PostgresServerDbContext DbContext { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        protected string? ErrorMessage { get; set; }

        private TemplateModel _templateModel = new();

        protected async Task HandleCreate()
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(_templateModel.Title))
            {
                ErrorMessage = "Template name must not be empty";
                return;
            }

            var template = new BanTemplate
            {
                Title = _templateModel.Title,
            };

            DbContext.BanTemplate.Add(template);
            await DbContext.SaveChangesAsync();

            // Navigate to the edit page for the newly created template
            NavigationManager.NavigateTo($"/bans/templates/edit/{template.Id}");
        }

        public class TemplateModel
        {
            [Required]
            public string Title { get; set; } = "";
        }
    }
}
