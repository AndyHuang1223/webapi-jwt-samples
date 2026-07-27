using JwtCourseApi.Advanced.DTOs;
using JwtCourseApi.Advanced.Models;
using JwtCourseApi.Advanced.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtCourseApi.Advanced.Controllers;

[ApiController]
[Route("api/auth/body")]
public sealed class BodyAuthController(
    IDemoUserService demoUserService,
    IRefreshSessionService refreshSessionService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenPairResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = demoUserService.Authenticate(request.Username, request.Password);
        if (user is null)
        {
            return InvalidCredential();
        }

        var tokens = await refreshSessionService.CreateAsync(
            user,
            RefreshTokenTransport.Body,
            cancellationToken);

        return Ok(ToResponse(tokens));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenPairResponse>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await refreshSessionService.RotateAsync(
            request.RefreshToken,
            RefreshTokenTransport.Body,
            cancellationToken);

        return tokens is null
            ? InvalidCredential()
            : Ok(ToResponse(tokens));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var revoked = await refreshSessionService.RevokeAsync(
            request.RefreshToken,
            RefreshTokenTransport.Body,
            cancellationToken);

        return revoked ? NoContent() : InvalidCredential();
    }

    private static TokenPairResponse ToResponse(SessionTokenResult tokens)
    {
        return new TokenPairResponse(
            tokens.AccessToken,
            "Bearer",
            tokens.AccessTokenExpiresAtUtc,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAtUtc);
    }

    private UnauthorizedObjectResult InvalidCredential()
    {
        return Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "驗證失敗",
            Detail = "登入資訊或 Refresh Token 無效。"
        });
    }
}
