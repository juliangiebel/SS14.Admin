using System.ComponentModel.DataAnnotations;
using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Components.Pages.Bans.BanTemplates
{
    public partial class EditBanTemplate : ComponentBase
    {
        [Parameter]
        public int Id { get; set; }

        [Inject]
        protected PostgresServerDbContext DbContext { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        protected string? ErrorMessage { get; set; }
        protected string? SuccessMessage { get; set; }

        private BanTemplate? _template;
        private InputModel _inputModel = new();
        private bool _showDeleteConfirmation = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadTemplate();
        }

        private async Task LoadTemplate()
        {
            _template = await DbContext.BanTemplate.SingleOrDefaultAsync(t => t.Id == Id);

            if (_template == null)
            {
                ErrorMessage = "Template not found";
                return;
            }

            // Initialize input model with template values
            _inputModel = new InputModel
            {
                Title = _template.Title,
                LengthMinutes = (int)_template.Length.TotalMinutes,
                Reason = _template.Reason,
                Hidden = _template.Hidden,
                Severity = _template.Severity,
                ExemptFlags = _template.ExemptFlags
            };
        }

        protected async Task HandleSave()
        {
            ErrorMessage = null;
            SuccessMessage = null;

            if (_template == null)
            {
                ErrorMessage = "Template not found";
                return;
            }

            if (string.IsNullOrWhiteSpace(_inputModel.Title))
            {
                ErrorMessage = "Title must not be empty";
                return;
            }

            // Update template with input values
            _template.Title = _inputModel.Title;
            _template.Length = TimeSpan.FromMinutes(_inputModel.LengthMinutes);
            _template.Reason = _inputModel.Reason ?? "";
            _template.Hidden = _inputModel.Hidden;
            _template.Severity = _inputModel.Severity;
            _template.ExemptFlags = _inputModel.ExemptFlags;

            await DbContext.SaveChangesAsync();

            SuccessMessage = "Changes saved successfully";
        }

        protected void SetDuration(int minutes)
        {
            _inputModel.LengthMinutes = minutes;
        }

        private void ToggleExemptFlag(ServerBanExemptFlags flag, bool isChecked)
        {
            if (isChecked)
            {
                _inputModel.ExemptFlags |= flag;
            }
            else
            {
                _inputModel.ExemptFlags &= ~flag;
            }
        }

        protected void ShowDeleteConfirmation()
        {
            _showDeleteConfirmation = true;
        }

        protected void CancelDelete()
        {
            _showDeleteConfirmation = false;
        }

        protected async Task ConfirmDelete()
        {
            if (_template == null)
            {
                ErrorMessage = "Template not found";
                _showDeleteConfirmation = false;
                return;
            }

            try
            {
                DbContext.BanTemplate.Remove(_template);
                await DbContext.SaveChangesAsync();

                // Redirect back to the create ban page after successful deletion
                NavigationManager.NavigateTo("/bans/createban");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to delete template: {ex.Message}";
                _showDeleteConfirmation = false;
            }
        }

        public class InputModel
        {
            [Required]
            public string Title { get; set; } = "";

            public int LengthMinutes { get; set; }

            public string? Reason { get; set; }

            public bool Hidden { get; set; }

            public NoteSeverity Severity { get; set; }

            public ServerBanExemptFlags ExemptFlags { get; set; }
        }
    }
}
