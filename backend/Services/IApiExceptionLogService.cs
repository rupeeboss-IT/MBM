namespace RB_Website_API.Services;

public interface IApiExceptionLogService
{
    void LogInBackground(
        HttpContext context,
        Exception exception,
        int statusCode,
        string userMessage,
        string? operationKey);
}
