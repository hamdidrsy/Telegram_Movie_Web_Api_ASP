namespace TelegramMovieBot.Api.Clients;

public sealed class TelegramClient(HttpClient httpClient)
{
    public HttpClient HttpClient { get; } = httpClient;
}
