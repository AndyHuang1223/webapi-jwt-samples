using JwtCourseApi.Advanced.Models;
using Microsoft.EntityFrameworkCore;

namespace JwtCourseApi.Advanced.Data;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var refreshToken = modelBuilder.Entity<RefreshToken>();

        refreshToken.ToTable("RefreshTokens");
        refreshToken.HasKey(token => token.Id);
        refreshToken.Property(token => token.UserId).HasMaxLength(128).IsRequired();
        refreshToken.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
        refreshToken.Property(token => token.Transport)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        refreshToken.Property(token => token.RevokedReason).HasMaxLength(32);
        refreshToken.Property(token => token.Version).IsConcurrencyToken();

        refreshToken.HasIndex(token => token.TokenHash).IsUnique();
        refreshToken.HasIndex(token => token.FamilyId);
        refreshToken.HasIndex(token => token.UserId);
    }
}
