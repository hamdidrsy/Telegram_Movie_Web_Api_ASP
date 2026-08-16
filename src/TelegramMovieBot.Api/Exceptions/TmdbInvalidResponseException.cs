namespace TelegramMovieBot.Api.Exceptions;

public sealed class TmdbInvalidResponseException(string message) : Exception(message);
