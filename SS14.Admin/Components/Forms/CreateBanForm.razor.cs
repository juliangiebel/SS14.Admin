using System.ComponentModel.DataAnnotations;
using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SS14.Admin.Helpers;

namespace SS14.Admin.Components.Forms
{
    public partial class CreateBanForm : ComponentBase
    {
        [Inject]
        protected PostgresServerDbContext DbContext { get; set; } = default!;

        [Inject]
        protected BanHelper BanHelper { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        /// <summary>
        /// The model for creating a ban.
        /// </summary>
        [Parameter]
        public CreateBanModel BanModel { get; set; } = new CreateBanModel();

        /// <summary>
        /// Callback fired after a successful submission.
        /// </summary>
        [Parameter]
        public EventCallback<EditContext> OnSubmit { get; set; } = EventCallback<EditContext>.Empty;

        protected string? ErrorMessage { get; set; }
        protected string? SuccessMessage { get; set; }

        /// <summary>
        /// Handles valid form submission.
        /// </summary>
        protected async Task Submit(EditContext editContext)
        {


            // Clear previous messages.
            ErrorMessage = null;
            SuccessMessage = null;

            if ((BanModel.UseLatestHwid || BanModel.UseLatestIp) &&
                (BanModel.IP == null && BanModel.NameOrUid == null && BanModel.HWid == null))
            {
                ErrorMessage = "When using the latest hwid, or IP, you must specify at least one of, IP, HWID, or Name or UserID";
                return;
            }

            // Determine the IP and HWID values.
            string? ipAddr = BanModel.IP;
            string? hwid = BanModel.HWid;

            if (BanModel.UseLatestIp || BanModel.UseLatestHwid)
            {
                var lastInfo = await BanHelper.GetLastPlayerInfo(BanModel.NameOrUid);
                if (lastInfo == null)
                {
                    ErrorMessage = "Unable to retrieve latest player info for the provided Name/UID.";
                    return;
                }
                if (BanModel.UseLatestIp)
                {
                    ipAddr = lastInfo.Value.address.ToString();
                }
                if (BanModel.UseLatestHwid)
                {
                    hwid = lastInfo.Value.hwid?.ToString();
                }
            }

            // Create a new ban instance.
            var ban = new ServerBan
            {
                Hidden = BanModel.Hidden,
                Severity = BanModel.Severity,
                BanHits = new List<ServerBanHit>()
            };

            // Use the helper to fill common ban fields and perform validations.
            var error = await BanHelper.FillBanCommon(
                ban,
                BanModel.NameOrUid,
                ipAddr,
                hwid,
                BanModel.LengthMinutes,
                BanModel.Reason);

            if (error != null)
            {
                ErrorMessage = error;
                return;
            }

            //Technically if the banning admin is not in the player table it will crash the server, (This is an extremely rare case but helps with development)
            var isAdminReal = await BanHelper.GetLastPlayerInfo(ban.BanningAdmin.ToString());
            if (isAdminReal == null)
            {
                ErrorMessage = "Banning admin is not in the player Database";
                return;
            }

            // Save the ban to the database.
            DbContext.Ban.Add(ban);
            await DbContext.SaveChangesAsync();

            SuccessMessage = "Ban created successfully.";
            await OnSubmit.InvokeAsync(editContext);
        }

        /// <summary>
        /// The model for creating a ban.
        /// </summary>
        public partial class CreateBanModel
        {
            public string? NameOrUid { get; set; }
            public string? IP { get; set; }
            public string? HWid { get; set; }
            public bool UseLatestIp { get; set; }
            public bool UseLatestHwid { get; set; }
            public int LengthMinutes { get; set; }
            public string Reason { get; set; } = "";
            public bool Hidden { get; set; }
            public NoteSeverity Severity { get; set; }
            public DateTime? ExpirationTime { get; set; }
        }
        private void ClearError(ChangeEventArgs e)
        {
            ErrorMessage = null;
            StateHasChanged();
        }
    }
}
