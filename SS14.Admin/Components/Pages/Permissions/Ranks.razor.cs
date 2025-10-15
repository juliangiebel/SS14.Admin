using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Components.Pages.Permissions;

public partial class Ranks
{
    [Inject]
    private PostgresServerDbContext? Context { get; set; }

    public QuickGrid<RankViewModel>? Grid { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 10 };

    private GridItemsProvider<RankViewModel>? _rankProvider;

    protected override async Task OnInitializedAsync()
    {
        _rankProvider = async request =>
        {
            // Increase the count by one if it's not unlimited so we can check if there is a next page available
            var limit = request.Count + 1;
            var query = GetRankQuery();
            query = request.ApplySorting(query);
            query = query.Skip(request.StartIndex);

            if (limit != null)
                query = query.Take(limit.Value);

            // Materialize the query immediately to avoid DbContext disposal issues
            var page = await query.ToListAsync();

            if (page.Count == 0)
                return GridItemsProviderResult.From(page, request.StartIndex);

            // We assume that there's at least another page worth of items left if the amount of returned items
            // is more than the requested amount.
            var hasNextPage = request.Count != null && page.Count > request.Count;

            // Return the current count plus twice to returned items to signify that there is at least one more page.
            // If there is no next page, we return the current count plus the returned amount of items once.
            // This total item count mustn't be shown to the user but be used to decide if the next button gets disabled.
            var totalItemCount = request.StartIndex + (hasNextPage ? (page.Count - 1) * 2 : page.Count - 1);

            return GridItemsProviderResult.From(page, totalItemCount);
        };
    }

    private IQueryable<RankViewModel> GetRankQuery()
    {
        var query = from rank in Context.AdminRank.AsNoTracking()
                    orderby rank.Name
                    select new RankViewModel
                    {
                        Id = rank.Id,
                        Name = rank.Name,
                        Flags = rank.Flags
                    };

        return query;
    }

    private async Task Refresh()
    {
    }

    public class RankViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public List<AdminRankFlag> Flags { get; set; } = new();
    }
}
