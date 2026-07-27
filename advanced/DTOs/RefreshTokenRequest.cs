using System.ComponentModel.DataAnnotations;

namespace JwtCourseApi.Advanced.DTOs;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
