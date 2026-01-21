using System.Drawing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webapi.Controllers.Models;
using webapi.Services.GameInfra;

namespace webapi.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class GameController(GameService gameService) : Controller
{
    [Authorize(Policy = "ValidPlayer")]
    [HttpGet("sizes")]
    public IActionResult GetSizes()
    {
        var (width, height) = gameService.GetSizes();
        return Ok(new { width, height });
    }

    [Authorize(Policy = "ActivePlayer")]
    [HttpPost("set")]
    public IActionResult SetPixel([FromBody] SetPixelModel payload)
    {
        var color = Color.FromArgb(payload.Color);
        var player = HttpContext.Items["Player"] as Player;
        gameService.SetPixel(player!, payload.Point[0], payload.Point[1], color);
        return Ok();
    }
    
    [Authorize(Policy = "ValidPlayer")]
    [HttpGet("bitmap")]
    public IActionResult GetBitmap()
    {
        var (bitmap, _) = gameService.GetSavedState();
        return File(bitmap, "application/octet-stream");
    }
}