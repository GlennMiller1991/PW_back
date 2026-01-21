using Microsoft.AspNetCore.Mvc;
using webapi.Controllers.Models;
using webapi.Services;

namespace webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogController(TelegramService notificator): Controller
{
    [HttpPost("")]
    public void Log([FromBody] LogMessageModel msgModel)
    {
        if (string.IsNullOrEmpty(msgModel.Message)) return;
        
        notificator.SendTextMessage(msgModel.Beautiful);
    }
}