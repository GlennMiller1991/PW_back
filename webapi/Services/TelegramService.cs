using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using webapi.Services.GameInfra;

namespace webapi.Services;

public class TelegramService(
    IConfiguration configuration,
    GameService gameService,
    ImageMaker imageMaker
) : IBackgroundService
{
    private TelegramBotClient _bot;
    private int _adminId;
    private GameService _gameService;
    private int? _lastImageMsg;

    public async Task Run(CancellationToken stoppingToken)
    {
        var token = configuration.GetValue<string>("TELEGRAM_BOT_TOKEN");
        _adminId = configuration.GetValue<int>("TELEGRAM_ADMIN_ID");
        _gameService = gameService;

        if (string.IsNullOrEmpty(token))
        {
            throw new Exception($"telegram token is invalid");
        }

        _bot = new TelegramBotClient(token);
        Console.WriteLine("telegram bot is starting....");
        _bot.OnMessage += ReadMessage;

        await SendTextMessage("telegram bot just started");
    }

    public Task Exit()
    {
        SendTextMessage("Shutdown...");

        return Task.CompletedTask;
    }

    private async Task ReadMessage(Message message, UpdateType type)
    {
        if (message.From.Id != _adminId) return;

        var txt = message.Text ?? message.Caption;

        switch (txt)
        {
            case "/image":
                var msg = await SendImageWithClear();
                if (msg != null) _lastImageMsg = msg.Id;

                break;
            case "/clear":
                byte[]? bitmap = null;
                if (message.Photo?.Length > 0)
                {
                    var photo = message.Photo[^1];
                    var fileInfo = await _bot.GetFile(photo.FileId);
                    if (fileInfo.FilePath is not null)
                    {
                        using (var stream = new MemoryStream())
                        {
                            await _bot.DownloadFile(fileInfo.FilePath, stream);
                            stream.Position = 0;

                            bitmap = await ImageMaker.BmpToByteArr(stream, _gameService.GetSizes());
                        }
                    }
                }

                _gameService.Clear(bitmap);
                break;
        }

        DeleteMsgById(message.Id);
    }

    public Task<Message> SendTextMessage(string msg)
    {
        return _bot.SendMessage(
            chatId: _adminId,
            text: msg);
    }

    public async Task<Message?> SendImageWithClear()
    {
        var msg = await SendImageMessage();
        if (_lastImageMsg != null) DeleteMsgById((int)_lastImageMsg);

        return msg;
    }

    public async Task<Message?> SendImageMessage()
    {
        using (var stream = imageMaker.GetImage())

        {
            Message? msg = null;
            try
            {
                msg = await _bot.SendPhoto(
                    chatId: _adminId,
                    photo: new InputFileStream(stream, $"image_{DateTime.Now:yyyyMMdd_HHmmss}.bmp")
                );
            }
            catch (Exception e)
            {
            }

            return msg;
        }
    }

    private async void DeleteMsgById(int msgId)
    {
        try
        {
            await _bot.DeleteMessage(_adminId, msgId);
        }
        catch (Exception e)
        {
        }
    }
}