using System.ComponentModel.DataAnnotations;
using JwtCourseApi.Advanced.DTOs;
using JwtCourseApi.Advanced.Models;
using JwtCourseApi.Advanced.Options;
using JwtCourseApi.Advanced.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JwtCourseApi.Advanced.Controllers;

[ApiController]
[Route("api/auth/cookie")]
public sealed class CookieAuthController(
    IDemoUserService demoUserService,
    IRefreshSessionService refreshSessionService,
    IAntiforgery antiforgery,
    IOptions<RefreshTokenOptions> options) : ControllerBase
{
    private readonly RefreshTokenOptions _options = options.Value;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<CookieTokenResponse>> Login(
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
            RefreshTokenTransport.Cookie,
            cancellationToken);

        SetRefreshTokenCookie(tokens.RefreshToken, tokens.RefreshTokenExpiresAtUtc);
        return Ok(ToResponse(tokens));
    }

    [HttpGet("csrf")]
    [AllowAnonymous]
    public ActionResult<CsrfTokenResponse> Csrf()
    {
        return Ok(new CsrfTokenResponse(CreateCsrfToken()));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<CookieTokenResponse>> Refresh(
        [FromHeader(Name = "X-CSRF-TOKEN"), Required] string csrfToken,
        CancellationToken cancellationToken)
    {
        _ = csrfToken;
        var rawRefreshToken = Request.Cookies[_options.CookieName];
        var tokens = await refreshSessionService.RotateAsync(
            rawRefreshToken ?? string.Empty,
            RefreshTokenTransport.Cookie,
            cancellationToken);

        if (tokens is null)
        {
            DeleteRefreshTokenCookie();
            return InvalidCredential();
        }

        SetRefreshTokenCookie(tokens.RefreshToken, tokens.RefreshTokenExpiresAtUtc);
        return Ok(ToResponse(tokens));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(
        [FromHeader(Name = "X-CSRF-TOKEN"), Required] string csrfToken,
        CancellationToken cancellationToken)
    {
        _ = csrfToken;
        var rawRefreshToken = Request.Cookies[_options.CookieName];
        var revoked = await refreshSessionService.RevokeAsync(
            rawRefreshToken ?? string.Empty,
            RefreshTokenTransport.Cookie,
            cancellationToken);

        DeleteRefreshTokenCookie();
        return revoked ? NoContent() : InvalidCredential();
    }

    private CookieTokenResponse ToResponse(SessionTokenResult tokens)
    {
        return new CookieTokenResponse(
            tokens.AccessToken,
            "Bearer",
            tokens.AccessTokenExpiresAtUtc,
            tokens.RefreshTokenExpiresAtUtc,
            CreateCsrfToken());
    }

    private string CreateCsrfToken()
    {
        return antiforgery.GetAndStoreTokens(HttpContext).RequestToken
            ?? throw new InvalidOperationException("無法建立 antiforgery request token。");
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expiresAtUtc)
    {
        Response.Cookies.Append(
            _options.CookieName,
            refreshToken,
            CreateCookieOptions(new DateTimeOffset(expiresAtUtc)));
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(
            _options.CookieName,
            CreateCookieOptions(expires: null));
    }

    private static CookieOptions CreateCookieOptions(DateTimeOffset? expires)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth/cookie",
            Expires = expires
        };
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
