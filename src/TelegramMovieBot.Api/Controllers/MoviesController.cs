using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TelegramMovieBot.Api.Models;
using TelegramMovieBot.Api.Services;

namespace TelegramMovieBot.Api.Controllers;

[ApiController]
[Route("api/movies")]
public sealed class MoviesController(MovieService movieService) : ControllerBase
{
    [HttpGet("now-playing")]
    [ProducesResponseType<IReadOnlyList<Movie>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<Movie>>> GetNowPlaying(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var movies = await movieService.GetNowPlayingAsync(page, cancellationToken);
        return Ok(movies);
    }

    [HttpGet("upcoming")]
    [ProducesResponseType<IReadOnlyList<Movie>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<Movie>>> GetUpcoming(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var movies = await movieService.GetUpcomingAsync(page, cancellationToken);
        return Ok(movies);
    }
}
