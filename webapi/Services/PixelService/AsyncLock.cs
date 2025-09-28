namespace webapi.Services.PixelService;

public class AsyncLock
{
    public readonly Task Task = new(() => {});

    public void Complete()
    {
        Task.Start();
    }
}