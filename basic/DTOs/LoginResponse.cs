namespace JwtCourseApi.Basic.DTOs;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc);
