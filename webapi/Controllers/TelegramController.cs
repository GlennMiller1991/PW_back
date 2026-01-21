using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using webapi.Infrastructure.Repositories;
using webapi.Services.GameInfra;

namespace webapi.Controllers;

public class TelegramController
{
    private TelegramBotClient _bot;
    private readonly int _adminId;
    private readonly GameService _gameService;
    private int? _lastImageMsg;

    public TelegramController(IConfiguration configuration, GameService gameService)
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

        SendTextMessage("telegram bot just started");
    }

    private async Task ReadMessage(Message message, UpdateType type)
    {
        if (message.From.Id != _adminId) return;

        var txt = message.Text ?? message.Caption;

        switch (txt)
        {
            case "/image":
                var msg = await SendImageMessage();
                var lastImageMsg = _lastImageMsg;
                if (msg != null) _lastImageMsg = msg.Id;

                if (lastImageMsg != null) DeleteMsgById((int)lastImageMsg);

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

                            using (var img = await Image.LoadAsync(stream))
                            {
                                var (width, height) = _gameService.GetSizes();
                                img.Mutate(x => x.Resize(new ResizeOptions
                                {
                                    Size = new Size(width, height),
                                    Mode = ResizeMode.Stretch,
                                }));

                                using (var rgbImage = img.CloneAs<Rgb24>())
                                {
                                    bitmap = new byte[width * height * 3];
                                    rgbImage.CopyPixelDataTo(bitmap);
                                    _gameService.Clear(bitmap);
                                }
                            }
                        }
                    }
                }

                _gameService.Clear(bitmap);
                break;
        }

        DeleteMsgById(message.Id);
    }

    private Task<Message> SendTextMessage(string msg)
    {
        return _bot.SendMessage(
            chatId: _adminId,
            text: msg);
    }

    private async Task<Message?> SendImageMessage()
    {
        var (bitmap, _) = _gameService.GetSavedState();
        using var stream = ByteArrToBmp(bitmap);
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

    static Stream ByteArrToBmp(byte[] arr)
    {
        var len = arr.Length - 6;
        var size = Convert.ToInt32(Math.Sqrt(len / 3));

        using var image = new Image<Rgb24>(size, size);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var pixelIndex = (y * size + x) * 3;
                var r = arr[6 + pixelIndex];
                var g = arr[6 + pixelIndex + 1];
                var b = arr[6 + pixelIndex + 2];
                image[x, y] = new Rgb24(r, g, b);
            }
        }

        var stream = new MemoryStream();
        image.Save(stream, new BmpEncoder());
        stream.Position = 0;

        return stream;
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