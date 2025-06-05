using WasteWatchAIBackend.Data;
using System;
using WasteWatchAIBackend.Interface;
using WasteWatchAIBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace WasteWatchAIBackend.Repository
{
    public class WeatherRepository : IWeatherRepository
    {
        private readonly WasteWatchDbContext _context;

        public WeatherRepository(WasteWatchDbContext context)
        {
            _context = context;
        }

        public async Task SaveWeatherAsync(Weather data)
        {
            _context.Weather.Add(data);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Weather>> GetAllAsync()
        {
            return await _context.Weather.OrderByDescending(w => w.Timestamp).ToListAsync();
        }

    }

}
