namespace TelegramMovieBot.Api.Exceptions;

public sealed class TelegramApiException(
    string message,
    int? errorCode = null) : Exception(message)
{
    public int? ErrorCode { get; } = errorCode;
}
