using TelegramMovieBot.Api.Exceptions;
using TelegramMovieBot.Api.Infrastructure;

namespace TelegramMovieBot.Tests.Infrastructure;

public sealed class GlobalExceptionHandlerTests
{
    [Theory]
    [MemberData(nameof(ErrorScenarios))]
    public void MapException_ReturnsSafeExpectedStatus(
        Exception exception,
        bool requestAborted,
        int expectedStatus)
    {
        var result = GlobalExceptionHandler.MapException(exception, requestAborted);

        Assert.Equal(expectedStatus, result.Status);
        Assert.DoesNotContain("token", result.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    public static TheoryData<Exception, bool, int> ErrorScenarios => new()
    {
        { new ArgumentOutOfRangeException("page"), false, 400 },
        { new TmdbConfigurationException("Gizli token hatası"), false, 503 },
        { new TmdbInvalidResponseException("Ham cevap"), false, 502 },
        { new HttpRequestException("Dış servis ayrıntısı"), false, 502 },
        { new TaskCanceledException("Timeout ayrıntısı"), false, 504 },
        { new Exception("Stack ve dosya yolu"), false, 500 }
    };
}
