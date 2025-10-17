using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webapi.Extensions;
using webapi.Services;

namespace webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WsController(AuthService authService, WsConnectionManager manager) : Controller
{
    [HttpGet("upgrade")]
    public async Task<IActionResult> OpenWsConnection()
    {
        var token = Request.Cookies["ws-token"];
        if (token == null)
            return Unauthorized();

        var session = await authService.ValidateRefreshToken(token);
        HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
                    new Claim("session_version", session.Version.ToString())
                ],
                "WebSocket"
            )
        );

        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var websocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var userId = User.GetUserId();

            await manager.RemoveSocket(userId);
            await manager.AddSocket(websocket, userId);

            return Ok();
        }

        return BadRequest();
    }
}