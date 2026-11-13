using WeatherForecast.Models.OpenWeatherMap;

namespace OpenTelemetrySample.Services;

public interface IWeatherService
{
    Task<CurrentWeather> GetCurrentWeatherAsync(string city, CancellationToken cancellationToken = default);

    Task<DailyForecastWeather> GetWeatherForecastAsync(string city, int days, CancellationToken cancellationToken = default);
}
