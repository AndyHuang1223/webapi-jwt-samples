using JwtCourseApi.Advanced.Models;

namespace JwtCourseApi.Advanced.Services;

public interface IJwtTokenService
{
    AccessTokenResult CreateToken(DemoUser user);
}

public sealed record AccessTokenResult(string AccessToken, DateTime ExpiresAtUtc);
