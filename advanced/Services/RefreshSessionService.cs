using System.Security.Cryptography;
using System.Text;
using JwtCourseApi.Advanced.Data;
using JwtCourseApi.Advanced.Models;
using JwtCourseApi.Advanced.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JwtCourseApi.Advanced.Services;

public sealed class RefreshSessionService(
    AuthDbContext dbContext,
    IDemoUserService demoUserService,
    IJwtTokenService jwtTokenService,
    IOptions<RefreshTokenOptions> options,
    TimeProvider timeProvider) : IRefreshSessionService
{
    private const string RotatedReason = "Rotated";
    private const string ReplayReason = "ReplayDetected";
    private const string LogoutReason = "Logout";
    private const string ExpiredReason = "Expired";

    // SQLite is used for a single-process classroom sample. The concurrency token
    // still protects stale writes, while this gate makes the replay lesson deterministic.
    private static readonly SemaphoreSlim RotationGate = new(1, 1);

    private readonly RefreshTokenOptions _options = options.Value;

    public async Task<SessionTokenResult> CreateAsync(
        DemoUser user,
        RefreshTokenTransport transport,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var rawRefreshToken = GenerateRefreshToken();
        var refreshToken = CreateRefreshToken(
            user.Id,
            Guid.NewGuid(),
            transport,
            rawRefreshToken,
            nowUtc);

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateResult(user, rawRefreshToken, refreshToken.ExpiresAtUtc);
    }

    public async Task<SessionTokenResult?> RotateAsync(
        string rawRefreshToken,
        RefreshTokenTransport expectedTransport,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return null;
        }

        await RotationGate.WaitAsync(cancellationToken);
        try
        {
            return await RotateCoreAsync(rawRefreshToken, expectedTransport, cancellationToken);
        }
        finally
        {
            RotationGate.Release();
        }
    }

    public async Task<bool> RevokeAsync(
        string rawRefreshToken,
        RefreshTokenTransport expectedTransport,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return false;
        }

        await RotationGate.WaitAsync(cancellationToken);
        try
        {
            var tokenHash = HashToken(rawRefreshToken);
            var token = await dbContext.RefreshTokens
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.TokenHash == tokenHash &&
                            item.Transport == expectedTransport,
                    cancellationToken);

            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            if (token is null ||
                token.RevokedAtUtc is not null ||
                token.ExpiresAtUtc <= nowUtc)
            {
                return false;
            }

            await RevokeFamilyAsync(
                token.FamilyId,
                LogoutReason,
                nowUtc,
                cancellationToken);

            return true;
        }
        finally
        {
            RotationGate.Release();
        }
    }

    private async Task<SessionTokenResult?> RotateCoreAsync(
        string rawRefreshToken,
        RefreshTokenTransport expectedTransport,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(rawRefreshToken);
        var token = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(
                item => item.TokenHash == tokenHash &&
                        item.Transport == expectedTransport,
                cancellationToken);

        if (token is null)
        {
            return null;
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (token.RevokedAtUtc is not null)
        {
            if (token.RevokedReason == RotatedReason ||
                token.ReplacedByTokenId is not null)
            {
                await RevokeFamilyAsync(
                    token.FamilyId,
                    ReplayReason,
                    nowUtc,
                    cancellationToken);
            }

            return null;
        }

        if (token.ExpiresAtUtc <= nowUtc)
        {
            token.RevokedAtUtc = nowUtc;
            token.RevokedReason = ExpiredReason;
            token.Version = Guid.NewGuid();
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        var user = demoUserService.FindById(token.UserId);
        if (user is null)
        {
            await RevokeFamilyAsync(
                token.FamilyId,
                ReplayReason,
                nowUtc,
                cancellationToken);
            return null;
        }

        var replacementRawToken = GenerateRefreshToken();
        var replacement = CreateRefreshToken(
            token.UserId,
            token.FamilyId,
            token.Transport,
            replacementRawToken,
            nowUtc);

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        token.RevokedAtUtc = nowUtc;
        token.RevokedReason = RotatedReason;
        token.ReplacedByTokenId = replacement.Id;
        token.Version = Guid.NewGuid();
        dbContext.RefreshTokens.Add(replacement);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();

            await RevokeFamilyAsync(
                token.FamilyId,
                ReplayReason,
                nowUtc,
                cancellationToken);

            return null;
        }

        return CreateResult(user, replacementRawToken, replacement.ExpiresAtUtc);
    }

    private async Task RevokeFamilyAsync(
        Guid familyId,
        string reason,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken)
    {
        var version = Guid.NewGuid();

        await dbContext.RefreshTokens
            .Where(token =>
                token.FamilyId == familyId &&
                token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAtUtc, revokedAtUtc)
                    .SetProperty(token => token.RevokedReason, reason)
                    .SetProperty(token => token.Version, version),
                cancellationToken);
    }

    private RefreshToken CreateRefreshToken(
        string userId,
        Guid familyId,
        RefreshTokenTransport transport,
        string rawRefreshToken,
        DateTime createdAtUtc)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(rawRefreshToken),
            FamilyId = familyId,
            Transport = transport,
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = createdAtUtc.AddDays(_options.ExpirationDays),
            Version = Guid.NewGuid()
        };
    }

    private SessionTokenResult CreateResult(
        DemoUser user,
        string rawRefreshToken,
        DateTime refreshTokenExpiresAtUtc)
    {
        var accessToken = jwtTokenService.CreateToken(user);

        return new SessionTokenResult(
            accessToken.AccessToken,
            accessToken.ExpiresAtUtc,
            rawRefreshToken,
            refreshTokenExpiresAtUtc);
    }

    private static string GenerateRefreshToken()
    {
        return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    private static string HashToken(string rawRefreshToken)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken)));
    }
}
