using Content.Shared.Database;

namespace SS14.Admin.AdminLogs
{
    public static class AdminLogImpactExtension
    {
        public static string CssColorClass(this LogImpact impact, string prefix = "text") => impact switch
        {
            LogImpact.Low => $"{prefix}-impact-severity-low",
            LogImpact.Medium => $"{prefix}-impact-severity-medium",
            LogImpact.High => $"{prefix}-impact-severity-high",
            LogImpact.Extreme => $"{prefix}-impact-severity-extreme",
            _ => ""
        };
}
}
