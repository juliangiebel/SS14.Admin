namespace SS14.Admin.Models;

public class BansFilterModel
{
    public string? Search { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public DateTime? ExpiresFrom { get; set; }
    public DateTime? ExpiresTo { get; set; }
    public bool ShowActive { get; set; } = true;
    public bool ShowExpired { get; set; } = true;
}
