using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webapi.Services.PixelService;

namespace webapi.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class GameController(PixelService pixelService) : Controller
{
    [Authorize(Policy = "ValidSession")]
    [HttpPost("action")]
    public async Task<IActionResult> Act()
    {
        Console.WriteLine(User.Claims);
        return Ok();
    }
}