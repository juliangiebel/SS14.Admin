using Content.Server.Database;

namespace SS14.Admin.Models;

public class ConnectionsFilterModel
{
    public string? Search { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? ServerId { get; set; }
    public bool ShowAccepted { get; set; } = true;
    public bool ShowBanned { get; set; } = true;
    public bool ShowWhitelist { get; set; } = true;
    public bool ShowFull { get; set; } = true;
    public bool ShowPanic { get; set; } = true;
    public bool ShowBabyJail { get; set; } = true;
    public bool ShowIPChecks { get; set; } = true;
}
