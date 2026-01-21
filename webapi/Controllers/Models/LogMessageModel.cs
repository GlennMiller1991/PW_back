namespace webapi.Controllers.Models;

public enum LogLevel
{
    Error = 0,
    Warning = 1,
    Info = 2,
}

public class LogMessageModel
{
    public string Message { get; set; }
    public LogLevel Level { get; set; } = LogLevel.Error;

    public string Beautiful
    {
        get
        {
            switch (Level)
            {
                case LogLevel.Error:
                    return $"Error was raised in app with given message:\n{Message}";
                default:
                    return Message;
            }
        }
    }
}