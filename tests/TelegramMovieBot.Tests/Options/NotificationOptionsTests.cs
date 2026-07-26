using System.ComponentModel.DataAnnotations;
using TelegramMovieBot.Api.Options;

namespace TelegramMovieBot.Tests.Options;

public sealed class NotificationOptionsTests
{
    [Fact]
    public void Defaults_AreValid()
    {
        var options = new NotificationOptions();
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            validationResults,
            validateAllProperties: true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void InvalidHour_FailsValidation()
    {
        var options = new NotificationOptions { Hour = 24 };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, result =>
            result.MemberNames.Contains(nameof(NotificationOptions.Hour)));
    }
}
