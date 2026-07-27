namespace JwtCourseApi.Advanced.DTOs;

public sealed record TokenPairResponse(
    string AccessToken,
    string TokenType,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
