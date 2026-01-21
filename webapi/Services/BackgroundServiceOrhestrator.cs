namespace webapi.Services;

public interface IBackgroundService
{
    Task Run(CancellationToken stoppingToken);
    Task Exit();
}

public class BackgroundServiceOrchestrator(
    TelegramService telegramService,
    BitmapBackupRestorationService bitmapBackupRestorationService
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var taskSource = new TaskCompletionSource();
        stoppingToken.Register(() => taskSource.SetResult());

        IBackgroundService[] tasks =
        [
            telegramService,
            bitmapBackupRestorationService,
        ];

        foreach (var task in tasks)
        {
            try
            {
                await task.Run(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{task.GetType()} was thrown an Error on START stage");
            }
        }

        await taskSource.Task;

        foreach (var task in tasks.Reverse())
            try
            {
                await task.Exit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{task.GetType()} was thrown an Error on EXIT stage");
            }
    }
}