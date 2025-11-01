using System.Drawing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webapi.Controllers.Models;
using webapi.Services;
using webapi.Services.GameService;

namespace webapi.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class GameController(GameService gameService) : Controller
{
    [HttpGet("sizes")]
    public IActionResult GetSizes()
    {
        var (width, height) = gameService.GetSizes();
        return Ok(new {width, height});
    }
    
    [Authorize(Policy = "ValidPlayer")]
    [HttpPost("set")]
    public async Task<IActionResult> SetPixel([FromBody] SetPixelModel payload)
    {
        var color = Color.FromArgb(payload.Color);
        var player = HttpContext.Items["Player"] as Player;
        await gameService.SetPixel(player!, payload.Point[0], payload.Point[1], color);
        return Ok();
    }

    [HttpGet("bitmap")]
    public IActionResult GetBitmap()
    {
        return File(gameService.GetBitmap(), "application/octet-stream");
    }
}