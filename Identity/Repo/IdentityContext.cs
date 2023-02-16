using Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Identity.Repo;

public class IdentityContext : DbContext
{
    public IdentityContext(DbContextOptions<IdentityContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Account>().HasKey(e => e.Id);
        modelBuilder.Entity<Account>().Property(e => e.Id).UseMySqlIdentityColumn();
        modelBuilder.Entity<Account>().HasMany<User>(e => e.Users).WithOne(e => e.Account);
        modelBuilder.Entity<Account>().HasIndex(e => e.Name);

        modelBuilder.Entity<User>().HasKey(e => e.Id);

        modelBuilder.Entity<LandscapeUrl>().HasKey(e => e.Id);
        modelBuilder.Entity<LandscapeUrl>().Property(e => e.Id).UseMySqlIdentityColumn();
        modelBuilder.Entity<LandscapeUrl>().HasIndex(e => e.Scope);
    }

    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<LandscapeUrl> LandscapeUrls { get; set; } = null!;
}
