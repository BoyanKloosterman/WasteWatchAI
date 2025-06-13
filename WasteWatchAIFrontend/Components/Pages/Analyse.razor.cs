using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.ComponentModel.DataAnnotations;
using WasteWatchAIFrontend.Models;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace WasteWatchAIFrontend.Components.Pages
{
    public partial class Analyse : ComponentBase
    {
        [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        // State variables
        private List<TrashItem> filteredTrashItems = new();
        private List<TrashItem> trashItems = new();
        private List<LocationChartData> locationChartData = new();
        private List<FrequencyDataItem> frequencyData = new();
        private readonly Dictionary<string, string> cameraLocationCache = new();
        private readonly Dictionary<string, string> locationCache = new();

        private bool isLoading = true;
        private string selectedPeriod = string.Empty;
        private string selectedLocation = string.Empty;
        private string selectedCategory = string.Empty;
        private CorrelationData? correlationData;
        private bool isLoadingCorrelation = false;
        private string correlationError = string.Empty;
        private bool useDummyData = false;
        private bool chartsNeedUpdate = false;
        private bool isDataModeChanging = false;
        private List<string> availableLocations = new();
        private readonly Dictionary<string, string> wasteTypeColors = new()
        {
            { "Plastic", "#e74c3c" },      // Red
            { "Papier", "#3498db" },       // Blue
            { "Organisch", "#2ecc71" },    // Green
            { "Glas", "#f39c12" },         // Orange
        };

        protected override async Task OnInitializedAsync()
        {
            if (useDummyData)
                await LoadDummyTrashData();
            else
                await LoadRealTrashData();

            // Pre-load detailed locations for real data
            if (!useDummyData && trashItems.Any())
            {
                await PreloadLocationNames();
            }

            await ProcessData();
            await LoadCorrelationData();
        }

        private async Task LoadDummyTrashData()
        {
            try
            {
                var httpClient = HttpClientFactory.CreateClient("WasteWatchAPI");
                var response = await httpClient.GetAsync("api/trashitems/dummy");
                if (response.IsSuccessStatusCode)
                {
                    trashItems = await response.Content.ReadFromJsonAsync<List<TrashItem>>() ?? new();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading trash data: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                UpdateAvailableLocations(); // Update available locations
                await ProcessData();
                chartsNeedUpdate = true;
                StateHasChanged();
            }
        }

        private async Task LoadRealTrashData()
        {
            isLoading = true;
            try
            {
                var httpClient = HttpClientFactory.CreateClient("WasteWatchAPI");
                var response = await httpClient.GetAsync("api/trashitems/trash");

                if (response.IsSuccessStatusCode)
                {
                    trashItems = await response.Content.ReadFromJsonAsync<List<TrashItem>>() ?? new();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading real trash data: {ex.Message}");
                trashItems = new();
            }
            finally
            {
                isLoading = false;
                UpdateAvailableLocations(); // Update available locations
                await ProcessData();
                StateHasChanged();
                chartsNeedUpdate = true;
            }
        }

        private async Task ToggleDataMode()
        {
            isLoading = true;
            isDataModeChanging = true; // Set flag before changing mode
            StateHasChanged();

            // Reset filters when switching data mode
            ResetFiltersWithoutProcessing();

            // Clear location caches when switching modes
            cameraLocationCache.Clear();
            locationCache.Clear();

            if (useDummyData)
                await LoadDummyTrashData();
            else
                await LoadRealTrashData();

            await LoadCorrelationData();
            
            isDataModeChanging = false; // Reset flag after mode change is complete
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!isLoading && (firstRender || chartsNeedUpdate))
            {
                await JS.InvokeVoidAsync("eval", @"
                var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle=""tooltip""]'));
                tooltipTriggerList.map(function (tooltipTriggerEl) {
                    return new bootstrap.Tooltip(tooltipTriggerEl);
                });
            ");

                await InitializeCharts();
                chartsNeedUpdate = false;
            }
        }

        private async Task InitializeCharts()
        {
            await InitializeTypeDistributionChart();
            await InitializeFrequencyChart();
        }

        private async Task InitializeTypeDistributionChart()
        {
            if (!locationChartData.Any()) return;

            var locations = locationChartData.Select(l => l.LocationName).ToArray();
            var wasteTypes = GetAllWasteTypes();

            var datasets = wasteTypes.Select(wasteType => new
            {
                label = wasteType,
                data = locationChartData.Select(loc => loc.TypeCounts.GetValueOrDefault(wasteType, 0)).ToArray(),
                backgroundColor = wasteTypeColors.GetValueOrDefault(wasteType, "#95a5a6"),
                borderColor = wasteTypeColors.GetValueOrDefault(wasteType, "#95a5a6"),
                borderWidth = 1
            }).ToArray();

            var chartData = new
            {
                labels = locations,
                datasets = datasets
            };

            await JS.InvokeVoidAsync("initializeTypeDistributionChart", chartData);
        }

        private async Task InitializeFrequencyChart()
        {
            if (!frequencyData.Any()) return;

            var chartData = new
            {
                labels = frequencyData.Select(f => f.Time).ToArray(),
                datasets = new[]
                {
                    new
                    {
                        label = "Detecties",
                        data = frequencyData.Select(f => f.Value).ToArray(),
                        borderColor = "#4299e1",
                        backgroundColor = "rgba(66, 153, 225, 0.3)",
                        fill = true,
                        tension = 0.4,
                        pointBackgroundColor = "#4299e1",
                        pointBorderColor = "#ffffff",
                        pointBorderWidth = 2,
                        pointRadius = 4
                    }
                }
            };

            await JS.InvokeVoidAsync("initializeFrequencyChart", chartData);
        }

        private async Task ProcessData()
        {
            if (!trashItems.Any()) return;

            // Process location-based chart data
            var locationGroups = trashItems
                .GroupBy(item => GetLocationName(item.Latitude, item.Longitude))
                .Select(group => new LocationChartData
                {
                    LocationName = group.Key,
                    TypeCounts = group.GroupBy(item => item.LitterType)
                        .ToDictionary(typeGroup => typeGroup.Key, typeGroup => typeGroup.Count()),
                    TotalCount = group.Count()
                })
                .OrderByDescending(loc => loc.TotalCount)
                .Take(5) // Show top 5 locations
                .ToList();

            locationChartData = locationGroups;

            // Process frequency data - count by hour of day
            var hourlyFrequency = new List<FrequencyDataItem>();

            // Create hourly buckets from 06:00 to 22:00
            for (int hour = 6; hour <= 22; hour++)
            {
                var count = trashItems.Count(item => item.Timestamp.Hour == hour);
                hourlyFrequency.Add(new FrequencyDataItem
                {
                    Label = $"{hour:D2}:00",
                    Time = $"{hour:D2}:00",
                    Value = count
                });
            }

            frequencyData = hourlyFrequency;
        }
        private void UpdateAvailableLocations()
        {
            // Always use the original unfiltered trashItems for available locations
            availableLocations = trashItems
                .Select(item => GetLocationName(item.Latitude, item.Longitude))
                .Distinct()
                .OrderBy(loc => loc)
                .ToList();
        }

      private string GetLocationName(float latitude, float longitude)
{
    if (!useDummyData)
    {
        var roundedLat = Math.Round(latitude, 4);
        var roundedLon = Math.Round(longitude, 4);
        var locationKey = $"{roundedLat},{roundedLon}";
        
        if (!cameraLocationCache.ContainsKey(locationKey))
        {
            var cameraNumber = cameraLocationCache.Count + 1;
            
            // Check if we have detailed location in cache
            if (locationCache.ContainsKey(locationKey))
            {
                var detailedLocation = locationCache[locationKey];
                cameraLocationCache[locationKey] = $"Camera {cameraNumber} - {detailedLocation}";
            }
            else
            {
                // Use improved fallback while API loads
                var detailedLocation = GetImprovedLocationFallback(latitude, longitude);
                cameraLocationCache[locationKey] = $"Camera {cameraNumber} - {detailedLocation}";
            }
        }
        
        return cameraLocationCache[locationKey];
    }

    // Breda: Grote Markt (updated range)
    if (latitude >= 51.5890 && latitude <= 51.5900 && longitude >= 4.7750 && longitude <= 4.7765)
        return "Grote Markt Breda";

    // Breda: Centraal Station
    if (latitude >= 51.5953 && latitude <= 51.5963 && longitude >= 4.7787 && longitude <= 4.7797)
        return "Centraal Station Breda";

    // Breda: Valkenberg Park
    if (latitude >= 51.5929 && latitude <= 51.5939 && longitude >= 4.7791 && longitude <= 4.7801)
        return "Valkenberg Park";

    // Breda: Haagdijk (updated range)
    if (latitude >= 51.5920 && latitude <= 51.5925 && longitude >= 4.7685 && longitude <= 4.7695)
        return "Haagdijk";

    // Breda: Chassé Park (new)
    if (latitude >= 51.5860 && latitude <= 51.5866 && longitude >= 4.7848 && longitude <= 4.7856)
        return "Chassé Park";

    // Breda: Chasséveld (existing)
    if (latitude >= 51.5890 && latitude <= 51.5902 && longitude >= 4.7750 && longitude <= 4.7766)
        return "Chasséveld";

    // Default locations for other dummy coordinates
    var random = new Random((int)(latitude * 1000 + longitude * 1000));
    var locations = new[] { "Stadspark", "Marktplein", "Winkelcentrum", "Sportpark", "Industrieterrein" };
    return locations[random.Next(locations.Length)];
}

private string GetImprovedLocationFallback(float latitude, float longitude)
{
    // More precise Breda locations using exact coordinates
    
    // Chasséveld Breda (51.58894, 4.78522)
    if (latitude >= 51.588 && latitude <= 51.590 && longitude >= 4.784 && longitude <= 4.786)
        return "Chasséveld, Breda";
    
    // Grote Markt Breda
    if (latitude >= 51.588 && latitude <= 51.591 && longitude >= 4.774 && longitude <= 4.777)
        return "Grote Markt, Centrum, Breda";
    
    // Centraal Station Breda
    if (latitude >= 51.595 && latitude <= 51.597 && longitude >= 4.778 && longitude <= 4.780)
        return "Stationsplein, Breda Centraal";
    
    // Valkenberg Park
    if (latitude >= 51.592 && latitude <= 51.595 && longitude >= 4.778 && longitude <= 4.781)
        return "Valkenberg Park, Breda";
    
    // Haagdijk
    if (latitude >= 51.591 && latitude <= 51.594 && longitude >= 4.767 && longitude <= 4.770)
        return "Haagdijk, Breda";
    
    // Chassé Park
    if (latitude >= 51.585 && latitude <= 51.587 && longitude >= 4.784 && longitude <= 4.786)
        return "Chassé Park, Breda";
    
    // Broader Breda area
    if (latitude >= 51.55 && latitude <= 51.62 && longitude >= 4.73 && longitude <= 4.82)
        return "Breda";
    
    // Other major Dutch cities with more precise ranges
    if (latitude >= 52.35 && latitude <= 52.38 && longitude >= 4.88 && longitude <= 4.92)
        return "Amsterdam Centrum";
    
    if (latitude >= 51.91 && latitude <= 51.93 && longitude >= 4.46 && longitude <= 4.49)
        return "Rotterdam Centrum";
    
    if (latitude >= 52.06 && latitude <= 52.08 && longitude >= 4.29 && longitude <= 4.32)
        return "Den Haag Centrum";
    
    // Use more precise coordinates in fallback
    return $"Nederland ({Math.Round(latitude, 3)}, {Math.Round(longitude, 3)})";
}

        private string GetDayAbbreviation(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "Ma",
                DayOfWeek.Tuesday => "Di",
                DayOfWeek.Wednesday => "Wo",
                DayOfWeek.Thursday => "Do",
                DayOfWeek.Friday => "Vr",
                DayOfWeek.Saturday => "Za",
                DayOfWeek.Sunday => "Zo",
                _ => day.ToString()
            };
        }

        private int GetDayOrder(string dayAbbr)
        {
            return dayAbbr switch
            {
                "Ma" => 1,
                "Di" => 2,
                "Wo" => 3,
                "Do" => 4,
                "Vr" => 5,
                "Za" => 6,
                "Zo" => 7,
                _ => 8
            };
        }

        private string GetFilterSummary()
    {
        var parts = new List<string>();

        // Periode
        if (!string.IsNullOrEmpty(selectedPeriod))
        {
            parts.Add(selectedPeriod switch
            {
                "week" => "de afgelopen week",
                "month" => "de afgelopen maand",
                "year" => "het afgelopen jaar",
                _ => ""
            });
        }
        else
        {
            parts.Add("de volledige periode");
        }

        // Locatie
        if (!string.IsNullOrEmpty(selectedLocation))
            parts.Add($"locatie: {selectedLocation}");
        else
            parts.Add("alle locaties");

        // Categorie
        if (!string.IsNullOrEmpty(selectedCategory))
        {
            var cat = selectedCategory switch
            {
                "plastic" => "Plastic",
                "papier" => "Papier",
                "gft" => "GFT/Organisch",
                "glas" => "Glas",
                _ => selectedCategory
            };
            parts.Add($"categorie: {cat}");
        }
        else
        {
            parts.Add("alle categorieën");
        }

        // Add item count if filters are active
        if (HasActiveFilters())
        {
            var itemCount = filteredTrashItems?.Count ?? 0;
            parts.Add($"({itemCount} items)");
        }
        else
        {
            parts.Add($"({trashItems.Count} items)");
        }

        return string.Join(", ", parts);
    }


 private async Task ApplyFilters()
    {
        var filteredItems = trashItems.AsEnumerable();

        if (!string.IsNullOrEmpty(selectedPeriod))
        {
            var now = DateTime.Now;
            filteredItems = selectedPeriod switch
            {
                "week" => filteredItems.Where(item => item.Timestamp >= now.AddDays(-7)),
                "month" => filteredItems.Where(item => item.Timestamp >= now.AddMonths(-1)),
                "year" => filteredItems.Where(item => item.Timestamp >= now.AddYears(-1)),
                _ => filteredItems
            };
        }

        if (!string.IsNullOrEmpty(selectedCategory))
        {
            var categoryMap = new Dictionary<string, string>
            {
                { "plastic", "Plastic" },
                { "papier", "Papier" },
                { "gft", "Organisch" },
                { "glas", "Glas" }
            };

            if (categoryMap.TryGetValue(selectedCategory, out var actualCategory))
            {
                filteredItems = filteredItems.Where(item =>
                    item.LitterType.Equals(actualCategory, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (!string.IsNullOrEmpty(selectedLocation))
        {
            filteredItems = filteredItems.Where(item =>
                GetLocationName(item.Latitude, item.Longitude).Equals(selectedLocation, StringComparison.OrdinalIgnoreCase));
        }

        filteredTrashItems = filteredItems.ToList();
        
        var originalItems = trashItems;
        trashItems = filteredTrashItems;

        await ProcessData();
        await LoadCorrelationData();
        trashItems = originalItems;

        StateHasChanged();
        await InitializeCharts();
    }

        private async Task OnFilterChanged()
        {
            // Only check if selected location exists when changing data modes
            if (isDataModeChanging && !string.IsNullOrEmpty(selectedLocation) && !availableLocations.Contains(selectedLocation))
            {
                selectedLocation = string.Empty;
            }

            await ApplyFilters();
        }

        private async Task ResetFilters()
        {
            ResetFiltersWithoutProcessing();
            await ApplyFilters();
        }

        private void ResetFiltersWithoutProcessing()
        {
            selectedPeriod = string.Empty;
            selectedLocation = string.Empty;
            selectedCategory = string.Empty;
        }

        private bool HasActiveFilters()
        {
            return !string.IsNullOrEmpty(selectedPeriod) ||
                   !string.IsNullOrEmpty(selectedLocation) ||
                   !string.IsNullOrEmpty(selectedCategory);
        }

        private int GetMaxValueForLocation(LocationChartData location)
        {
            return location.TypeCounts.Values.DefaultIfEmpty(0).Max();
        }

        private List<string> GetAllWasteTypes()
        {
            return locationChartData
                .SelectMany(loc => loc.TypeCounts.Keys)
                .Distinct()
                .OrderBy(type => type)
                .ToList();
        }

         private async Task LoadCorrelationData()
    {
        isLoadingCorrelation = true;
        correlationError = string.Empty;

        try
        {
            Console.WriteLine("Starting correlation data load...");
            var httpClient = HttpClientFactory.CreateClient();

            // Use filtered items if filters are active, otherwise use all items
            var itemsToAnalyze = HasActiveFilters() ? filteredTrashItems : trashItems;
            
            // Prepare trash items data (use filtered or all data based on filters)
            var trashItemsToSend = itemsToAnalyze
                .Select(item => new
                {
                    id = item.Id.ToString(),
                    litterType = item.LitterType,
                    latitude = item.Latitude,
                    longitude = item.Longitude,
                    timestamp = item.Timestamp
                }).ToList();

            Console.WriteLine($"Sending {trashItemsToSend.Count} trash items to API (filtered: {HasActiveFilters()})");

            var daysBack = selectedPeriod switch
            {
                "week" => 7,
                "month" => 31,
                "year" => 365,
                _ => 31
            };

            var requestData = new
            {
                trash_items = trashItemsToSend,
                latitude = 51.5912, // Breda coordinates
                longitude = 4.7761,
                days_back = daysBack
            };

            Console.WriteLine($"Making API request with {daysBack} days back...");
            var response = await httpClient.PostAsJsonAsync("http://localhost:8000/api/correlation/analyze", requestData);

            Console.WriteLine($"API Response status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response received - Length: {responseContent.Length} characters");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                };

                correlationData = JsonSerializer.Deserialize<CorrelationData>(responseContent, options);

                if (correlationData != null)
                {
                    Console.WriteLine($"Successfully parsed correlation data:");
                    Console.WriteLine($"  - Coefficient: {correlationData.CorrelationCoefficient}");
                    Console.WriteLine($"  - Strength: {correlationData.CorrelationStrength}");
                    Console.WriteLine($"  - Sunny: {correlationData.SunnyWeatherPercentage}%");
                    Console.WriteLine($"  - Rainy: {correlationData.RainyWeatherPercentage}%");
                    Console.WriteLine($"  - Temperature data points: {correlationData.ChartData.TemperatureData.Temperature.Count}");
                    Console.WriteLine($"  - Insights count: {correlationData.Insights.Count}");
                    Console.WriteLine($"  - Using filtered data: {HasActiveFilters()}");

                    // Force state change to render HTML
                    StateHasChanged();

                    // Wait longer for DOM to update and start chart initialization in background
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1000); // Wait 1 second for DOM to be ready
                        await InvokeAsync(async () =>
                        {
                            await InitializeCorrelationChart();
                        });
                    });
                }
                else
                {
                    correlationError = "Failed to parse correlation data from API response";
                    Console.WriteLine(correlationError);
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                correlationError = $"API Error: {response.StatusCode} - {errorContent}";
                Console.WriteLine($"API Error - Status: {response.StatusCode}, Content: {errorContent}");
            }
        }
        catch (HttpRequestException httpEx)
        {
            correlationError = $"Network error: {httpEx.Message}. Make sure the FastAPI server is running on http://localhost:8000";
            Console.WriteLine($"HTTP Exception in LoadCorrelationData: {httpEx}");
        }
        catch (JsonException jsonEx)
        {
            correlationError = $"JSON parsing error: {jsonEx.Message}";
            Console.WriteLine($"JSON Exception in LoadCorrelationData: {jsonEx}");
        }
        catch (Exception ex)
        {
            correlationError = $"Unexpected error: {ex.Message}";
            Console.WriteLine($"General Exception in LoadCorrelationData: {ex}");
        }
        finally
        {
            isLoadingCorrelation = false;
            StateHasChanged();
        }
    }

        private async Task InitializeCorrelationChart()
        {
            if (correlationData?.ChartData == null)
            {
                Console.WriteLine("No correlation data available");
                return;
            }

            Console.WriteLine("Starting chart initialization...");

            // Wait for DOM elements to exist with retry mechanism
            var maxRetries = 10;
            var retryCount = 0;
            bool elementsExist = false;

            while (!elementsExist && retryCount < maxRetries)
            {
                await Task.Delay(200); // Wait 200ms between checks

                try
                {
                    var correlationExists = await JS.InvokeAsync<bool>("eval", "document.getElementById('correlationChart') !== null");
                    var weatherExists = await JS.InvokeAsync<bool>("eval", "document.getElementById('weatherDistributionChart') !== null");
                    var scatterExists = await JS.InvokeAsync<bool>("eval", "document.getElementById('scatterChart') !== null");

                    Console.WriteLine($"Retry {retryCount + 1}: Canvas elements exist - Correlation: {correlationExists}, Weather: {weatherExists}, Scatter: {scatterExists}");

                    if (correlationExists && weatherExists && scatterExists)
                    {
                        elementsExist = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking canvas elements on retry {retryCount + 1}: {ex.Message}");
                }

                retryCount++;
            }

            if (!elementsExist)
            {
                Console.WriteLine("Canvas elements not found after maximum retries. Charts will not be initialized.");
                return;
            }

            Console.WriteLine("Canvas elements found, initializing charts...");

            try
            {
                // Initialize main correlation chart
                var chartData = new
                {
                    labels = correlationData.ChartData.TemperatureData.Labels.ToArray(),
                    datasets = new object[]
                    {
                        new
                        {
                            label = "Temperatuur (°C)",
                            data = correlationData.ChartData.TemperatureData.Temperature.ToArray(),
                            borderColor = "#ff6b6b",
                            backgroundColor = "rgba(255, 107, 107, 0.1)",
                            yAxisID = "y",
                            type = "line"
                        },
                        new
                        {
                            label = "Afval Items",
                            data = correlationData.ChartData.TemperatureData.TrashCount.ToArray(),
                            borderColor = "#4ecdc4",
                            backgroundColor = "rgba(78, 205, 196, 0.3)",
                            yAxisID = "y1",
                            type = "bar"
                        }
                    }
                };

                await JS.InvokeVoidAsync("initializeCorrelationChart", chartData);
                Console.WriteLine("Main correlation chart initialized successfully");

                // Initialize weather distribution chart
                if (correlationData.ChartData.WeatherDistribution?.Labels?.Any() == true)
                {
                    var weatherChartData = new
                    {
                        labels = correlationData.ChartData.WeatherDistribution.Labels.ToArray(),
                        values = correlationData.ChartData.WeatherDistribution.Values.ToArray()
                    };

                    await JS.InvokeVoidAsync("initializeWeatherDistributionChart", weatherChartData);
                    Console.WriteLine("Weather distribution chart initialized successfully");
                }

                // Initialize scatter plot
                if (correlationData.ChartData.CorrelationScatter?.Temperature?.Any() == true)
                {
                    var scatterData = new
                    {
                        datasets = new object[]
                        {
                            new
                            {
                                label = "Temperatuur vs Afval",
                                data = correlationData.ChartData.CorrelationScatter.Temperature
                                    .Zip(correlationData.ChartData.CorrelationScatter.TrashCount, (temp, trash) => new { x = temp, y = trash })
                                    .ToArray(),
                                backgroundColor = "rgba(75, 192, 192, 0.6)",
                                borderColor = "rgba(75, 192, 192, 1)",
                                borderWidth = 1
                            }
                        }
                    };

                    await JS.InvokeVoidAsync("initializeScatterChart", scatterData);
                    Console.WriteLine("Scatter chart initialized successfully");
                }

                Console.WriteLine("All charts initialization completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing correlation charts: {ex.Message}");
            }
        }

private async Task<string> GetDetailedLocationAsync(float latitude, float longitude)
{
    var locationKey = $"{Math.Round(latitude, 4)},{Math.Round(longitude, 4)}";
    
    // Check cache first
    if (locationCache.ContainsKey(locationKey))
        return locationCache[locationKey];

    try
    {
        using var httpClient = HttpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "WasteWatchAI/1.0");
        
        // Use higher precision for the API call
        var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude:F5}&lon={longitude:F5}&addressdetails=1&zoom=18";
        
        var response = await httpClient.GetStringAsync(url);
        var data = JsonSerializer.Deserialize<JsonElement>(response);
        
        if (!data.TryGetProperty("address", out var address))
        {
            var fallback = GetImprovedLocationFallback(latitude, longitude);
            locationCache[locationKey] = fallback;
            return fallback;
        }
        
        // Build detailed location string
        var locationParts = new List<string>();
        
        // Add street info (prioritize road over other types)
        if (address.TryGetProperty("road", out var road))
        {
            var streetName = road.GetString();
            if (address.TryGetProperty("house_number", out var houseNumber))
                locationParts.Add($"{streetName} {houseNumber.GetString()}");
            else
                locationParts.Add(streetName);
        }
        else if (address.TryGetProperty("pedestrian", out var pedestrian))
        {
            locationParts.Add(pedestrian.GetString());
        }
        else if (address.TryGetProperty("amenity", out var amenity))
        {
            locationParts.Add(amenity.GetString());
        }
        else if (address.TryGetProperty("leisure", out var leisure))
        {
            locationParts.Add(leisure.GetString());
        }
        
        // Add neighborhood/suburb
        if (address.TryGetProperty("neighbourhood", out var neighbourhood))
            locationParts.Add(neighbourhood.GetString());
        else if (address.TryGetProperty("suburb", out var suburb))
            locationParts.Add(suburb.GetString());
        else if (address.TryGetProperty("quarter", out var quarter))
            locationParts.Add(quarter.GetString());
        
        // Add city
        string city = null;
        if (address.TryGetProperty("city", out var cityProp))
            city = cityProp.GetString();
        else if (address.TryGetProperty("town", out var town))
            city = town.GetString();
        else if (address.TryGetProperty("village", out var village))
            city = village.GetString();
        else if (address.TryGetProperty("municipality", out var municipality))
            city = municipality.GetString();
        
        if (!string.IsNullOrEmpty(city))
            locationParts.Add(city);
        
        // Combine parts intelligently
        var result = string.Join(", ", locationParts.Where(p => !string.IsNullOrEmpty(p)).Distinct());
        
        if (string.IsNullOrEmpty(result))
            result = GetImprovedLocationFallback(latitude, longitude);
        
        locationCache[locationKey] = result;
        return result;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error getting detailed location: {ex.Message}");
        var fallback = GetImprovedLocationFallback(latitude, longitude);
        locationCache[locationKey] = fallback;
        return fallback;
    }
}

// Update PreloadLocationNames to update camera cache after API calls
private async Task PreloadLocationNames()
{
    var uniqueCoordinates = trashItems
        .Select(item => new { item.Latitude, item.Longitude })
        .Distinct()
        .ToList();

    foreach (var coord in uniqueCoordinates)
    {
        try
        {
            var detailedLocation = await GetDetailedLocationAsync(coord.Latitude, coord.Longitude);
            
            // Update camera cache with detailed location
            var roundedLat = Math.Round(coord.Latitude, 4);
            var roundedLon = Math.Round(coord.Longitude, 4);
            var locationKey = $"{roundedLat},{roundedLon}";
            
            // Find existing camera entry and update it
            var existingCamera = cameraLocationCache.FirstOrDefault(kvp => kvp.Key == locationKey);
            if (!existingCamera.Equals(default(KeyValuePair<string, string>)))
            {
                var cameraNumber = existingCamera.Value.Split(' ')[1]; // Extract camera number
                cameraLocationCache[locationKey] = $"Camera {cameraNumber} - {detailedLocation}";
            }
            
            // Small delay to be respectful to the API
            await Task.Delay(200);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error preloading location for {coord.Latitude}, {coord.Longitude}: {ex.Message}");
        }
    }
    
    // Trigger UI update after all locations are loaded
    StateHasChanged();
}
    }
}