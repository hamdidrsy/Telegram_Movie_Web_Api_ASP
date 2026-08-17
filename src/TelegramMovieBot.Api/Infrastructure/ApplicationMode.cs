namespace TelegramMovieBot.Api.Infrastructure;

public static class ApplicationMode
{
    public const string RunOnceArgument = "--run-once";

    public static bool IsRunOnce(IEnumerable<string> arguments) =>
        arguments.Any(argument =>
            string.Equals(
                argument,
                RunOnceArgument,
                StringComparison.OrdinalIgnoreCase));
}
