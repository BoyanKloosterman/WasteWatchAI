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
        private List<TrashItem> trashItems = new();
        private List<LocationChartData> locationChartData = new();
        private List<FrequencyDataItem> frequencyData = new();
        private bool isLoading = true;
        private string selectedPeriod = string.Empty;
        private string selectedLocation = string.Empty;
        private string selectedCategory = string.Empty;
        private CorrelationData? correlationData;
        private bool isLoadingCorrelation = false;
        private string correlationError = string.Empty;
        private bool useDummyData = false;
        private bool chartsNeedUpdate = false;

        // Define color mapping for waste types
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
                await ProcessData();
                StateHasChanged();
                chartsNeedUpdate = true;
            }
        }

        private async Task ToggleDataMode()
        {
            isLoading = true;
            StateHasChanged();

            if (useDummyData)
                await LoadDummyTrashData();
            else
                await LoadRealTrashData();

            await LoadCorrelationData();
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

        private string GetLocationName(float latitude, float longitude)
        {
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

            // Default locations for other coordinates
            var random = new Random((int)(latitude * 1000 + longitude * 1000));
            var locations = new[] { "Stadspark", "Marktplein", "Winkelcentrum", "Sportpark", "Industrieterrein" };
            return locations[random.Next(locations.Length)];
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

            var originalItems = trashItems;
            trashItems = filteredItems.ToList();

            await ProcessData();
            trashItems = originalItems;

            StateHasChanged();
            await InitializeCharts();
        }

        // New method for handling filter changes
        private async Task OnFilterChanged()
        {
            await ApplyFilters();
        }

        // New method for resetting filters
        private async Task ResetFilters()
        {
            selectedPeriod = string.Empty;
            selectedLocation = string.Empty;
            selectedCategory = string.Empty;

            await ApplyFilters();
        }

        // Helper method to check if any filters are active
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

                // Prepare trash items data (use dummy or real based on toggle)
                var trashItemsToSend = trashItems
                    .Select(item => new
                    {
                        id = item.Id.ToString(),
                        litterType = item.LitterType,
                        latitude = item.Latitude,
                        longitude = item.Longitude,
                        timestamp = item.Timestamp
                    }).ToList();

                Console.WriteLine($"Sending {trashItemsToSend.Count} trash items to API");

                var requestData = new
                {
                    trash_items = trashItemsToSend,
                    latitude = 51.5912, // Breda coordinates
                    longitude = 4.7761,
                    days_back = 30
                };

                Console.WriteLine("Making API request...");
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
                        Console.WriteLine($"  - Weather distribution labels: {correlationData.ChartData.WeatherDistribution.Labels.Count}");
                        Console.WriteLine($"  - Correlation scatter points: {correlationData.ChartData.CorrelationScatter.Temperature.Count}");
                        Console.WriteLine($"  - Insights count: {correlationData.Insights.Count}");

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
    }
}