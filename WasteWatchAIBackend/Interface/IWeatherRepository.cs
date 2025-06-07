using WasteWatchAIBackend.Models;

namespace WasteWatchAIBackend.Interface
{
    public interface IWeatherRepository
    {
        Task SaveWeatherAsync(WeatherData data);
        Task<IEnumerable<WeatherData>> GetAllAsync();
    }
}
