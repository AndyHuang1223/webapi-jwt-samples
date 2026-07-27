using System.ComponentModel.DataAnnotations;

namespace JwtCourseApi.Advanced.Options;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    [Range(1, 365)]
    public int ExpirationDays { get; init; } = 30;

    [Required]
    public string CookieName { get; init; } = "__Secure-jwt-refresh";
}
