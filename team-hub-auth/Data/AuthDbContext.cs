using Microsoft.EntityFrameworkCore;
using team_hub_auth.Models;

namespace team_hub_auth.Data;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.RefreshTokenHash).IsUnique();
        });
    }
}
