using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Models;

namespace SS14.Admin.Components.Pages.Permissions;

public partial class AdminPermissions
{
    [Inject]
    private IDbContextFactory<PostgresServerDbContext>? ContextFactory { get; set; }

    private readonly AdminFilterModel _filter = new();
    public QuickGrid<AdminViewModel>? Grid { get; set; }

    private PaginationState _pagination = new() { ItemsPerPage = 10 };

    private GridItemsProvider<AdminViewModel>? _adminProvider;

    //this is basically the same as the logs request and needs to be refactored ot be generalized
    protected override async Task OnInitializedAsync()
    {
        _adminProvider = async request =>
        {
            await using var context = await ContextFactory!.CreateDbContextAsync();

            // Increase the count by one if it's not unlimited so we can check if there is a next page available
            var limit = request.Count + 1;
            var query = GetAdminQuery(context);
            query = request.ApplySorting(query);
            query = query.Skip(request.StartIndex);

            if (limit != null)
                query = query.Take(limit.Value);

            // Materialize the query immediately to avoid DbContext disposal issues
            // if not it will grab a disposed context and break
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

    private IQueryable<AdminViewModel> GetAdminQuery(PostgresServerDbContext context)
    {
        var query = from admin in context.Admin.AsNoTracking()
                    join player in context.Player.AsNoTracking() on admin.UserId equals player.UserId
                    join rank in context.AdminRank.AsNoTracking() on admin.AdminRankId equals rank.Id into rankJoin
                    from r in rankJoin.DefaultIfEmpty()
                    orderby player.LastSeenUserName
                    select new AdminViewModel
                    {
                        UserId = admin.UserId,
                        Username = player.LastSeenUserName,
                        Title = admin.Title ?? "none",
                        Rank = r != null ? r.Name : null
                    };

        // Apply filters this should reflect AdminFilter.cs params
        if (!string.IsNullOrWhiteSpace(_filter.Search))
        {
            query = query.Where(a => a.Username.Contains(_filter.Search));
        }

        if (!string.IsNullOrWhiteSpace(_filter.Rank))
        {
            query = query.Where(a => a.Rank != null && a.Rank.Contains(_filter.Rank));
        }

        return query;
    }

    private async Task Refresh()
    {
    }

    private async Task RefreshFilter()
    {
        await Grid.RefreshDataAsync();
    }

    public class AdminViewModel
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = "";
        public string Title { get; set; } = "none";
        public string? Rank { get; set; }

        public Dictionary<string, bool> Flags { get; set; } = new();
    }
}
