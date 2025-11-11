namespace SS14.Admin.Models;

public class PlayerFilterModel
{
    public string? Search { get; set; }
    public DateTime? FirstSeenFrom { get; set; }
    public DateTime? FirstSeenTo { get; set; }
    public DateTime? LastSeenFrom { get; set; }
    public DateTime? LastSeenTo { get; set; }
}
