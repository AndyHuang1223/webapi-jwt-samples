using JwtCourseApi.Basic.Models;

namespace JwtCourseApi.Basic.Services;

public interface IJwtTokenService
{
    TokenResult CreateToken(DemoUser user);
}

public sealed record TokenResult(string AccessToken, DateTime ExpiresAtUtc);
