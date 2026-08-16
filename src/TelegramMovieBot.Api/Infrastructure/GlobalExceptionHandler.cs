using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TelegramMovieBot.Api.Exceptions;

namespace TelegramMovieBot.Api.Infrastructure;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var error = MapException(exception, httpContext.RequestAborted.IsCancellationRequested);

        logger.Log(
            error.Status >= StatusCodes.Status500InternalServerError
                ? LogLevel.Error
                : LogLevel.Warning,
            exception,
            "İstek işlenirken hata oluştu. HTTP durum kodu: {StatusCode}",
            error.Status);

        var problemDetails = new ProblemDetails
        {
            Status = error.Status,
            Title = error.Title,
            Detail = error.Detail,
            Instance = httpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = error.Status;
        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }

    public static ErrorDetails MapException(Exception exception, bool requestAborted) =>
        exception switch
        {
            ArgumentOutOfRangeException => new(
                StatusCodes.Status400BadRequest,
                "Geçersiz istek",
                "Sayfa numarası 1 veya daha büyük olmalıdır."),

            TmdbConfigurationException => new(
                StatusCodes.Status503ServiceUnavailable,
                "Film servisi yapılandırılmamış",
                "Film servisi şu anda kullanılamıyor."),

            TmdbInvalidResponseException => new(
                StatusCodes.Status502BadGateway,
                "Film servisinden geçersiz cevap",
                "Film sağlayıcısından geçerli bir cevap alınamadı."),

            TelegramConfigurationException => new(
                StatusCodes.Status503ServiceUnavailable,
                "Telegram servisi yapılandırılmamış",
                "Bildirim servisi şu anda kullanılamıyor."),

            TelegramApiException => new(
                StatusCodes.Status502BadGateway,
                "Telegram mesajı gönderilemedi",
                "Telegram servisi mesajı kabul etmedi."),

            HttpRequestException => new(
                StatusCodes.Status502BadGateway,
                "Film servisine ulaşılamadı",
                "Film sağlayıcısıyla iletişim kurulamadı."),

            OperationCanceledException when !requestAborted => new(
                StatusCodes.Status504GatewayTimeout,
                "Film servisi zaman aşımına uğradı",
                "Film sağlayıcısı zamanında cevap vermedi."),

            _ => new(
                StatusCodes.Status500InternalServerError,
                "Beklenmeyen bir hata oluştu",
                "İstek işlenirken beklenmeyen bir sorun oluştu.")
        };

    public sealed record ErrorDetails(int Status, string Title, string Detail);
}
