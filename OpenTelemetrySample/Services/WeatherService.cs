using WeatherForecast.Models.OpenWeatherMap;

namespace OpenTelemetrySample.Services;

public class WeatherService(HttpClient httpClient, IConfiguration configuration) : IWeatherService
{
    public async Task<CurrentWeather> GetCurrentWeatherAsync(string city, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<CurrentWeather>($"weather?q={city}&appid={configuration.GetValue<string>("AppSettings:OpenWeatherMapAppId")}&units=metric", cancellationToken);
        return response!;
    }

    public async Task<DailyForecastWeather> GetWeatherForecastAsync(string city, int days, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<DailyForecastWeather>($"forecast/daily?q={city}&cnt={Math.Clamp(days, 1, 16)}&appid={configuration.GetValue<string>("AppSettings:OpenWeatherMapAppId")}&units=metric", cancellationToken);
        return response!;
    }
}
