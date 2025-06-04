using WasteWatchAIBackend.Model;

namespace WasteWatchAIBackend.Interface
{
    public interface IWeatherRepository
    {
        Task SaveWeatherAsync(Weather data);
        Task<IEnumerable<Weather>> GetAllAsync();
    }
}
