using Content.Server.Database;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Models;

namespace SS14.Admin.Components.Pages.Logs;

public partial class Logs
{
    [Inject]
    private IDbContextFactory<PostgresServerDbContext>? ContextFactory { get; set; }

    private readonly LogsFilterModel _filter = new();
    public QuickGrid<AdminLog>? Grid { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 50 };

    private GridItemsProvider<AdminLog>? _logsProvider;

    protected override async Task OnInitializedAsync()
    {
        _logsProvider = async request =>
        {
            await using var context = await ContextFactory!.CreateDbContextAsync();

            // 🤡
            // Increase the count by one if it's not unlimited so we can check if there is a next page available
            var limit  = request.Count + 1;
            var query = LogsQuery(context);
            query = request.ApplySorting(query);
            query = query.Skip(request.StartIndex);

            if (limit != null)
                query = query.Take(limit.Value);

            var page = await query.ToListAsync();

            if (page.Count == 0)
                GridItemsProviderResult.From(page,  request.StartIndex);

            // We asume that there's at least another page worth of items left if the amount of returned items
            // is more than the requested amount.
            var hasNextPage = request.Count != null && page.Count > request.Count;

            // Return the current count plus twice to returned items to signify that there is at least one more page.
            // If there is no next page, we return the current count plus the returned amount of items once.
            // This total item count mustn't be shown to the user but be used to decide if the next button gets disabled.
            var totalItemCount = request.StartIndex + (hasNextPage ?  (page.Count - 1) * 2 : page.Count - 1);

            return GridItemsProviderResult.From(page,  totalItemCount);
        };

    }

    private IQueryable<AdminLog> LogsQuery(PostgresServerDbContext context)
    {
        var query = context.AdminLog
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

        if (_filter.RoundId != null)
        {
            query = query.Where(log => log.RoundId == _filter.RoundId);
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

    private async Task RefreshFilter()
    {
        await Grid.RefreshDataAsync();
    }
}
