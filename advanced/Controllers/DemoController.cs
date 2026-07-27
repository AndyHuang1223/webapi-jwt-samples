using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtCourseApi.Advanced.Controllers;

[ApiController]
[Route("api/demo")]
public sealed class DemoController : ControllerBase
{
    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult Public()
    {
        return Ok(new { message = "這是公開 API，不需要 Bearer Token。" });
    }

    [HttpGet("profile")]
    [Authorize]
    public IActionResult Profile()
    {
        var claims = User.Claims
            .Select(claim => new { type = claim.Type, value = claim.Value })
            .ToArray();

        return Ok(new
        {
            userId = User.FindFirstValue("sub"),
            username = User.FindFirstValue("preferred_username"),
            displayName = User.FindFirstValue("name"),
            role = User.FindFirstValue("role"),
            department = User.FindFirstValue("department"),
            claims
        });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "只有 Admin 可以看到這個回應。" });
    }

    [HttpGet("it-department")]
    [Authorize(Policy = "ItDepartmentOnly")]
    public IActionResult ItDepartmentOnly()
    {
        return Ok(new { message = "IT 部門使用者可以看到這個回應。" });
    }
}
