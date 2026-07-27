using JwtCourseApi.Advanced.Models;

namespace JwtCourseApi.Advanced.Services;

public interface IRefreshSessionService
{
    Task<SessionTokenResult> CreateAsync(
        DemoUser user,
        RefreshTokenTransport transport,
        CancellationToken cancellationToken = default);

    Task<SessionTokenResult?> RotateAsync(
        string rawRefreshToken,
        RefreshTokenTransport expectedTransport,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        string rawRefreshToken,
        RefreshTokenTransport expectedTransport,
        CancellationToken cancellationToken = default);
}

public sealed record SessionTokenResult(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
