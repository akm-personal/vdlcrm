using Microsoft.AspNetCore.Mvc;
using Vdlcrm.Interfaces;
using Vdlcrm.Model;
using Vdlcrm.Utilities;

namespace Vdlcrm.Web.Controllers.Account;

[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IRepository<WeatherForecast> _repository;
    private static readonly string[] Summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public WeatherForecastController(IRepository<WeatherForecast> repository)
    {
        _repository = repository;
    }

    [HttpGet("GetWeatherForecast")]
    public async Task<ActionResult<IEnumerable<WeatherForecast>>> GetWeatherForecast()
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                Summaries[Random.Shared.Next(Summaries.Length)]
            )).ToList();

        return Ok(forecast);
    }
}
