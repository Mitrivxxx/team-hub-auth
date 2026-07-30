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

            e.OwnsOne(u => u.Identity, oi =>
            {
                oi.Property(x => x.Username)
                    .HasColumnName("Username")
                    .IsRequired()
                    .HasColumnType("text");

                oi.Property(x => x.Email)
                    .HasColumnName("Email")
                    .IsRequired()
                    .HasColumnType("text");

                oi.HasIndex(x => x.Username).IsUnique();
                oi.HasIndex(x => x.Email).IsUnique().HasFilter("\"Email\" <> ''");
            });

            e.OwnsOne(u => u.Profile, op =>
            {
                op.Property(x => x.Name)
                    .HasColumnName("Name")
                    .IsRequired()
                    .HasColumnType("text");

                op.Property(x => x.Surname)
                    .HasColumnName("Surname")
                    .IsRequired()
                    .HasColumnType("text");
            });

            e.OwnsOne(u => u.Credentials, oc =>
            {
                oc.Property(x => x.PasswordHash)
                    .HasColumnName("Password")
                    .IsRequired()
                    .HasColumnType("text");
            });

            e.OwnsOne(u => u.Security, os =>
            {
                os.Property(x => x.FailedLoginAttempts)
                    .HasColumnName("FailedLoginAttempts")
                    .HasColumnType("integer");

                os.Property(x => x.LockoutUntil)
                    .HasColumnName("LockoutUntil")
                    .HasColumnType("timestamp with time zone");

                os.Property(x => x.RefreshTokenHash)
                    .HasColumnName("RefreshTokenHash")
                    .HasColumnType("text");

                os.Property(x => x.RefreshTokenExpiresAt)
                    .HasColumnName("RefreshTokenExpiresAt")
                    .HasColumnType("timestamp with time zone");

                os.HasIndex(x => x.RefreshTokenHash).IsUnique();
            });
        });
    }
}
