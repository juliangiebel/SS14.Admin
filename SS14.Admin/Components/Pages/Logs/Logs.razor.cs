using Content.Server.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.AdminLogs;
using SS14.Admin.Components.Shared.Filter;
using SS14.Admin.Helpers;
using SS14.Admin.Pages.Logs;

namespace SS14.Admin.Components.Pages.Logs;

public partial class Logs
{
    [Inject]
    private PostgresServerDbContext? Context { get; set; }

    public QuickGrid<AdminLog> Grid { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 13 };

    private IQueryable<AdminLog> LogsQuery() => Context!.AdminLog
        .Include(l => l.Round)
        .ThenInclude(l => l.Server);

    private IFilterModel _model = new LogsFilterModel();
    /*private async Task Next()
    {
        await _pagination.SetCurrentPageIndexAsync(Math.Min(_pagination.CurrentPageIndex + 1, _pagination.LastPageIndex ?? int.MaxValue));
        Console.WriteLine("Next");
    }

    private async Task Previous()
    {
        await _pagination.SetCurrentPageIndexAsync(Math.Max(0, _pagination.CurrentPageIndex - 1));
        Console.WriteLine("Previous");
    }*/
    private async Task Refresh()
    {
    }

    public class LogsFilterModel : IFilterModel
    {
        public string Test { get; set; } = "test";
    }
}
