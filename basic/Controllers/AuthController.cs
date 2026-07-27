using JwtCourseApi.Basic.DTOs;
using JwtCourseApi.Basic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtCourseApi.Basic.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IDemoUserService demoUserService,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        var user = demoUserService.Authenticate(request.Username, request.Password);

        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "登入失敗",
                Detail = "帳號或密碼錯誤。"
            });
        }

        var token = jwtTokenService.CreateToken(user);
        return Ok(new LoginResponse(token.AccessToken, "Bearer", token.ExpiresAtUtc));
    }
}
