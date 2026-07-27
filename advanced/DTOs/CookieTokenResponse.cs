namespace JwtCourseApi.Advanced.DTOs;

public sealed record CookieTokenResponse(
    string AccessToken,
    string TokenType,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc,
    string CsrfToken);
