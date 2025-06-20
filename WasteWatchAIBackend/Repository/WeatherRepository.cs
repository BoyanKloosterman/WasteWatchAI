﻿using WasteWatchAIBackend.Data;
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

        public async Task SaveWeatherAsync(WeatherData data)
        {
            _context.WeatherData.Add(data);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<WeatherData>> GetAllAsync()
        {
            return await _context.WeatherData.OrderByDescending(w => w.Timestamp).ToListAsync();
        }
        public async Task<bool> WeatherExistsAsync(DateTime date, double latitude, double longitude)
        {
            return await _context.WeatherData.AnyAsync(w =>
                w.Timestamp.Date == date.Date &&
                w.Latitude == latitude &&
                w.Longitude == longitude);
        }

    }

}
