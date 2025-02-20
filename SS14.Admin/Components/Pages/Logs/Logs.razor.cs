using Content.Server.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Models;

namespace SS14.Admin.Components.Pages.Logs;

public partial class Logs
{
    [Inject]
    private PostgresServerDbContext? Context { get; set; }


    private readonly LogsFilterModel _filter = new();

    public QuickGrid<AdminLog> Grid { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 13 };

    private IQueryable<AdminLog> LogsQuery()
    {
        var query = Context!.AdminLog
            .Include(l => l.Round)
            .ThenInclude(l => l.Server)
            .Where(log => true);

        if (!string.IsNullOrWhiteSpace(_filter.Search))
        {
            query = query.Where(log =>
                EF.Functions.ToTsVector("english", log.Message)
                    .Matches(EF.Functions.WebSearchToTsQuery("english", _filter.Search)));
        }

        if (_filter.Type != null)
        {
            query = query.Where(log => log.Type == _filter.Type);
        }

        if (_filter.Impact != null)
        {
            query = query.Where(log => log.Impact == _filter.Impact);
        }

        if (_filter.DateFrom != null)
        {
            query = query.Where(log => log.Date >= _filter.DateFrom);
        }

        if (_filter.DateTo != null)
        {
            query = query.Where(log => log.Date <= _filter.DateTo);
        }

        return query;
    }
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
}
