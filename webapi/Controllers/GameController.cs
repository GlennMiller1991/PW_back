using System.Drawing;
using Microsoft.AspNetCore.Mvc;
using webapi.Controllers.Models;
using webapi.Services.PixelService;

namespace webapi.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class GameController(PixelService pixelService) : Controller
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
        await pixelService.SetPixel(payload.Point[0], payload.Point[1], color);
        return Ok();
    }

    [HttpGet("bitmap")]
    public IActionResult GetBitmap()
    {
        return File(pixelService.GetBitmap(), "application/octet-stream");
    }
}