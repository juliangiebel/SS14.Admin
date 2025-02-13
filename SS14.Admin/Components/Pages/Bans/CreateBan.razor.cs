using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;

namespace SS14.Admin.Components.Pages.Bans;

public partial class CreateBan
{
    [Inject]
    public PostgresServerDbContext DbContext { get; set; } = default!;

    [Inject]
    public BanHelper BanHelper { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// Error message to display to the user.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Success message to display to the user.
    /// </summary>
    public string? SuccessMessage { get; set; }

    // Bound form model.
    public CreateBanModel _banModel { get; set; } = new();

    public List<BanTemplate> Templates { get; set; } = new();

    public bool IsDropdownOpen { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadTemplates();
    }

    /// <summary>
    /// Handles the form submission.
    /// </summary>
    private async Task HandleValidSubmit()
    {
        // Clear any previous messages.
        ErrorMessage = null;
        SuccessMessage = null;

        // Ensure that at least one identifier is provided.
        if (string.IsNullOrWhiteSpace(_banModel.NameOrUid) &&
            string.IsNullOrWhiteSpace(_banModel.IP) &&
            string.IsNullOrWhiteSpace(_banModel.HWid))
        {
            ErrorMessage = "Must provide at least one of Name/UID, IP address, or HWID.";
            return;
        }

        // If using the latest info flags, a Name/UID is required.
        if ((_banModel.UseLatestIp || _banModel.UseLatestHwid) &&
            string.IsNullOrWhiteSpace(_banModel.NameOrUid))
        {
            ErrorMessage = "Must provide Name/UID to retrieve the latest IP/HWID.";
            return;
        }

        // Determine IP and HWID values.
        // They come either directly from the form or, if requested, from the latest player info.
        string? ipAddr = _banModel.IP;
        string? hwid = _banModel.HWid;

        if (_banModel.UseLatestIp || _banModel.UseLatestHwid)
        {
            var lastInfo = await BanHelper.GetLastPlayerInfo(_banModel.NameOrUid);
            if (lastInfo == null)
            {
                ErrorMessage = "Unable to retrieve latest player info for the provided Name/UID.";
                return;
            }
            if (_banModel.UseLatestIp)
            {
                ipAddr = lastInfo.Value.address.ToString();
            }
            if (_banModel.UseLatestHwid)
            {
                hwid = lastInfo.Value.hwid?.ToString();
            }
        }

        // Create a new ban instance.
        // Other fields (player GUID, IP conversion, HWID conversion, expiration, etc.)
        // are set inside FillBanCommon.
        var ban = new ServerBan
        {
            Hidden = _banModel.Hidden,
            Severity = _banModel.Severity,
            BanHits = new List<ServerBanHit>()
        };

        // Let the helper fill in the common fields and perform further validation.
        var error = await BanHelper.FillBanCommon(
            ban,
            _banModel.NameOrUid,
            ipAddr,
            hwid,
            _banModel.LengthMinutes,
            _banModel.Reason);

        if (error != null)
        {
            ErrorMessage = error;
            return;
        }

        // Save the ban to the database.
        DbContext.Ban.Add(ban);
        await DbContext.SaveChangesAsync();

        SuccessMessage = "Ban created successfully.";
        NavigationManager.NavigateTo("/bans");
    }

    /// <summary>
    /// Toggles the dropdown for ban templates.
    /// </summary>
    private void ToggleDropdown() => IsDropdownOpen = !IsDropdownOpen;

    /// <summary>
    /// Navigates to the template usage page.
    /// </summary>
    /// <param name="templateId">ID of the template.</param>
    private void UseTemplate(int templateId)
    {
        NavigationManager.NavigateTo($"/bans/use-template/{templateId}");
    }

    /// <summary>
    /// Loads available ban templates from the database.
    /// </summary>
    private async Task LoadTemplates()
    {
        Templates = await DbContext.BanTemplate.ToListAsync();
    }

    public class CreateBanModel
    {
        public string? NameOrUid { get; set; }
        public string? IP { get; set; }
        public string? HWid { get; set; }
        public bool UseLatestIp { get; set; }
        public bool UseLatestHwid { get; set; }
        public int LengthMinutes { get; set; }

        [Required(ErrorMessage = "Reason is required.")]
        public string Reason { get; set; } = "";

        public bool Hidden { get; set; }
        public NoteSeverity Severity { get; set; }
        public DateTime? ExpirationTime { get; set; }
    }
}
