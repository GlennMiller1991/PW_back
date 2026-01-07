using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webapi.Extensions;
using webapi.Services.AuthService;

namespace webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService) : Controller
{
    [HttpGet("accessibility")]
    public IActionResult IsServerAccessible() => Ok();

    [HttpGet("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var token = Request.Cookies["refresh-token"];
        if (token == null)
            return Unauthorized();

        try
        {
            var (accessToken, refreshToken, expires) =
                await authService.Refresh(token);
            return GetAuthenticatedResponse(accessToken, refreshToken, expires);
        }
        catch (Exception ex)
        {
            return Unauthorized();
        }
    }

    [HttpPost("google")]
    public async Task<IActionResult> AuthWithGoogle([FromBody] string idToken)
    {
        try
        {
            var (accessToken, refreshToken, expires) =
                await authService.AuthWithGoogle(idToken);

            return GetAuthenticatedResponse(accessToken, refreshToken, expires);
        }
        catch (Exception ex)
        {
            return BadRequest(ex);
        }
    }

    [Authorize(Policy = "ValidSession")]
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await authService.Logout(User.GetUserId());
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest();
        }
    }

    private IActionResult GetAuthenticatedResponse(string accessToken, string refreshToken, DateTime expires)
    {
        Response.Cookies.Append(
            "refresh-token",
            refreshToken,
            new CookieOptions
            {
                Expires = expires,
                HttpOnly = true,
                Path = "/api/auth/refresh",
            }
        );

        Response.Cookies.Append(
            "ws-token",
            refreshToken,
            new CookieOptions
            {
                Expires = expires,
                HttpOnly = true,
                Path = "/api/ws/upgrade"
            }
        );
        return Ok(new { accessToken });
    }
}