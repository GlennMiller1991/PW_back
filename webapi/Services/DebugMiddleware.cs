namespace webapi.Services;

public class DebugMiddleware
{
    private readonly RequestDelegate _next;

    public DebugMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Продолжаем обработку
        await _next(context);
    }
}
