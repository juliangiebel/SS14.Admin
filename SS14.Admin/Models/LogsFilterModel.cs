using Content.Shared.Database;

namespace SS14.Admin.Models;

public class LogsFilterModel
{
    public string? Search { get; set; }
    public LogType? Type { get; set; }
    public LogImpact? Impact { get; set; }
    public int? RoundId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
