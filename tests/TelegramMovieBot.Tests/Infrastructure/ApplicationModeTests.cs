using TelegramMovieBot.Api.Infrastructure;

namespace TelegramMovieBot.Tests.Infrastructure;

public sealed class ApplicationModeTests
{
    [Theory]
    [InlineData("--run-once")]
    [InlineData("--RUN-ONCE")]
    public void IsRunOnce_WithRunOnceArgument_ReturnsTrue(string argument)
    {
        Assert.True(ApplicationMode.IsRunOnce([argument]));
    }

    [Fact]
    public void IsRunOnce_WithoutRunOnceArgument_ReturnsFalse()
    {
        Assert.False(ApplicationMode.IsRunOnce([]));
    }
}
