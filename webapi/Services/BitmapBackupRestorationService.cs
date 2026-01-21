using webapi.Services.GameInfra;

namespace webapi.Services;

public class BitmapBackupRestorationService(ImageMaker imageMaker, GameService gameService) : IBackgroundService
{
    private const string _fileName = "backup.bmp";
    private int _backupVersion = -1;
    private int _cooldownMs = 1000 * 60 * 5;

    public async Task Run(CancellationToken cancellationToken)
    {
        await Restore();
        RunAsync(cancellationToken);
    }

    async Task RunAsync(CancellationToken cancellationToken)
    {
        
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_cooldownMs, cancellationToken);
            if (cancellationToken.IsCancellationRequested) break;

            await Backup(cancellationToken);

        }
    }

    private string BackupFilePath =>
        Path.Combine(Directory.GetCurrentDirectory(), $"backup/{_fileName}");

    private async Task Backup(CancellationToken? cancellationToken = null)
    {
        var (_, version) = gameService.GetSavedState();
        if (version <= _backupVersion) return;
        
        using (var stream = imageMaker.GetImage())
        {
            stream.Position = 0;
            var filePath = BackupFilePath;
            if (!File.Exists(filePath)) return;

            using (var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                await stream.CopyToAsync(fileStream, cancellationToken ?? CancellationToken.None);
            }
        }

        _backupVersion = version;

        return;
    }

    private async Task Restore()
    {
        var filePath = BackupFilePath;
        if (!File.Exists(filePath)) return;
        var memStream = new MemoryStream();
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            await fileStream.CopyToAsync(memStream, CancellationToken.None);
        }

        var bitmap = await ImageMaker.BmpToByteArr(memStream);
        gameService.Clear(bitmap);

    }

    public Task Exit()
    {
        return Backup();
    }
}