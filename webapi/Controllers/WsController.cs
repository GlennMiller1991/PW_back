using Microsoft.AspNetCore.Mvc;
using webapi.Services.AuthService;
using webapi.Services.GameService;

namespace webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WsController(AuthService authService, GameService gameService) : Controller
{
    [HttpGet("upgrade")]
    public async Task<IActionResult> OpenWsConnection()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
            return BadRequest();

        var token = Request.Cookies["ws-token"];
        if (token == null)
            return Unauthorized();

        var session = await authService.ValidateRefreshToken(token);
        using var websocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var player = gameService.AddPlayer(websocket, session.UserId);
        
        await player.Completion;
        return Ok();
    }
}