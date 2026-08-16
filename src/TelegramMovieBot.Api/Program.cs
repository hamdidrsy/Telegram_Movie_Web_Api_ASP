using System.Net.Http.Headers;
using TelegramMovieBot.Api.Clients;
using TelegramMovieBot.Api.Infrastructure;
using TelegramMovieBot.Api.Options;
using TelegramMovieBot.Api.Services;
using TelegramMovieBot.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<MovieService>();
builder.Services.AddScoped<TelegramMessageFormatter>();
builder.Services.AddScoped<IMovieNotificationService, MovieNotificationService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<NotificationSchedule>();
builder.Services.AddSingleton<INotificationDelay, NotificationDelay>();
builder.Services.AddSingleton<IScheduledNotificationDispatcher, ScheduledNotificationDispatcher>();
builder.Services.AddHostedService<MovieNotificationWorker>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services
    .AddOptions<TmdbOptions>()
    .Bind(builder.Configuration.GetSection(TmdbOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<TelegramOptions>()
    .Bind(builder.Configuration.GetSection(TelegramOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<NotificationOptions>()
    .Bind(builder.Configuration.GetSection(NotificationOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => NotificationSchedule.IsTimeZoneValid(options.TimeZone),
        "Notification:TimeZone geçerli bir saat dilimi olmalıdır.")
    .ValidateOnStart();

builder.Services.AddHttpClient<TmdbClient>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<TmdbOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));

    if (!string.IsNullOrWhiteSpace(options.AccessToken))
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.AccessToken);
    }
});

builder.Services
    .AddHttpClient<TelegramClient>(client =>
    {
        client.BaseAddress = new Uri("https://api.telegram.org/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .RemoveAllLoggers();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
