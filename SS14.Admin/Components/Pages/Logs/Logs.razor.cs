using Content.Server.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.AdminLogs;
using SS14.Admin.Helpers;
using SS14.Admin.Pages.Logs;

namespace SS14.Admin.Components.Pages.Logs;

public partial class Logs
{
    [Inject]
    private PostgresServerDbContext? Context { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 12 };

    private IQueryable<AdminLog> LogsQuery() => Context!.AdminLog
        .Include(l => l.Round)
        .ThenInclude(l => l.Server);

    private void Test()
    {
        Console.WriteLine("test");
    }
}
