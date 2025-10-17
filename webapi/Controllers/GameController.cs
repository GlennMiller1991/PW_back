using System.Drawing;
using Microsoft.AspNetCore.Mvc;
using webapi.Controllers.Models;
using webapi.Services;
using webapi.Services.PixelService;

namespace webapi.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class GameController(PixelService pixelService, Broadcast broadcast) : Controller
{
    [HttpGet("sizes")]
    public IActionResult GetSizes()
    {
        var (width, height) = pixelService.GetSizes();
        return Ok(new {width, height});
    }
    
    [HttpPost("set")]
    public async Task<IActionResult> SetPixel([FromBody] SetPixelModel payload)
    {
        var color = Color.FromArgb(payload.Color);
        pixelService.SetPixel(payload.Point[0], payload.Point[1], color);
        await broadcast.BroadcastPixel(payload.Point[0], payload.Point[1], color.R, color.G, color.B);
        return Ok();
    }

    [HttpGet("bitmap")]
    public IActionResult GetBitmap()
    {
        return File(pixelService.GetBitmap(), "application/octet-stream");
    }
}