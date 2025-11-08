using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Helpers;

/// <summary>
/// Helper functions for working with whitelist entities.
/// </summary>
/// <seealso cref="Whitelist"/>
public static class WhitelistHelper
{
    /// <summary>
    /// Make a query that joins the whitelist table with the player table,
    /// if the player is known in the database.
    /// </summary>
    public static IQueryable<WhitelistJoin> MakeWhitelistJoin(PostgresServerDbContext dbContext)
    {
        var player = dbContext.Player;
        var whitelist = dbContext.Whitelist;

        return whitelist.LeftJoin(
            player,
            w => w.UserId,
            p => p.UserId,
            (w, p) => new WhitelistJoin { Whitelist = w, Player = p });
    }

    /// <summary>
    /// Add a user to the whitelist.
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="userId">User GUID to add</param>
    /// <returns>True if added, false if already whitelisted</returns>
    public static async Task<bool> AddWhitelistAsync(PostgresServerDbContext dbContext, Guid userId)
    {
        if (await IsWhitelistedAsync(dbContext, userId))
            return false;

        dbContext.Whitelist.Add(new Whitelist { UserId = userId });
        await dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Remove a user from the whitelist.
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="userId">User GUID to remove</param>
    /// <returns>True if removed, false if not found</returns>
    public static async Task<bool> RemoveWhitelistAsync(PostgresServerDbContext dbContext, Guid userId)
    {
        var whitelist = await dbContext.Whitelist.FindAsync(userId);
        if (whitelist == null)
            return false;

        dbContext.Whitelist.Remove(whitelist);
        await dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Check if a user is whitelisted.
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="userId">User GUID to check</param>
    /// <returns>True if whitelisted, false otherwise</returns>
    public static async Task<bool> IsWhitelistedAsync(PostgresServerDbContext dbContext, Guid userId)
    {
        return await dbContext.Whitelist.AnyAsync(w => w.UserId == userId);
    }

    public sealed class WhitelistJoin
    {
        public required Whitelist Whitelist { get; set; }
        public Player? Player { get; set; }

        public void Deconstruct(out Whitelist whitelist, out Player? player)
        {
            whitelist = Whitelist;
            player = Player;
        }
    };
}
