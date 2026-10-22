namespace Gjubrarko.Application.Common;

public class AppException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static AppException NotFound(string what) => new(404, $"{what} was not found.");
    public static AppException BadRequest(string message) => new(400, message);
    public static AppException Forbidden(string message) => new(403, message);
}
