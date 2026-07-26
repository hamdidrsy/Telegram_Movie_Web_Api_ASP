namespace TelegramMovieBot.Api.Clients;

public sealed class TmdbClient(HttpClient httpClient)
{
    public HttpClient HttpClient { get; } = httpClient;
}
