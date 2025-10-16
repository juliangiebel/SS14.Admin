using Microsoft.EntityFrameworkCore;
using SS14.Admin.Data.Models;

namespace SS14.Admin.Data;

/// <summary>
/// SQLite database context for storing local user preferences.
/// This is a dummy implementation that will be implemtned at a bigger scale later more so a proof of concept
/// </summary>
public class PreferencesDbContext : DbContext
{
    public PreferencesDbContext(DbContextOptions<PreferencesDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// User preferences table
    /// </summary>
    public DbSet<UserPreference> UserPreferences { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.LastUpdated).IsRequired();
        });
    }
}
