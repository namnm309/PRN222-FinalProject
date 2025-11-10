using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MapsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public MapsController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public class PlaceResult
        {
            public string Title { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double? Rating { get; set; }
            public int? Reviews { get; set; }
            public string Address { get; set; }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] double lat, [FromQuery] double lng, [FromQuery] int zoom = 14)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query is required");
            }

            var apiKey = _configuration["SerpApi:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_SERPAPI_KEY_HERE")
            {
                return StatusCode(500, "SerpApi key is not configured");
            }

            var client = _httpClientFactory.CreateClient();
            var ll = $"@{lat},{lng},{zoom}z";
            var url = $"https://serpapi.com/search.json?engine=google_maps&q={Uri.EscapeDataString(query)}&ll={Uri.EscapeDataString(ll)}&api_key={Uri.EscapeDataString(apiKey)}";

            using var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Failed to fetch from SerpApi");
            }

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var list = new List<PlaceResult>();
            if (root.TryGetProperty("local_results", out var localResults) && localResults.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in localResults.EnumerateArray())
                {
                    try
                    {
                        string title = item.TryGetProperty("title", out var t) ? t.GetString() : null;
                        string address = item.TryGetProperty("address", out var a) ? a.GetString() : null;
                        double? rating = item.TryGetProperty("rating", out var r) && r.ValueKind == JsonValueKind.Number ? r.GetDouble() : (double?)null;
                        int? reviews = item.TryGetProperty("reviews", out var rv) && rv.ValueKind == JsonValueKind.Number ? rv.GetInt32() : (int?)null;

                        double latitude = 0;
                        double longitude = 0;
                        if (item.TryGetProperty("gps_coordinates", out var gps) && gps.ValueKind == JsonValueKind.Object)
                        {
                            if (gps.TryGetProperty("latitude", out var la) && la.ValueKind == JsonValueKind.Number)
                            {
                                latitude = la.GetDouble();
                            }
                            if (gps.TryGetProperty("longitude", out var lo) && lo.ValueKind == JsonValueKind.Number)
                            {
                                longitude = lo.GetDouble();
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(title) && latitude != 0 && longitude != 0)
                        {
                            list.Add(new PlaceResult
                            {
                                Title = title,
                                Latitude = latitude,
                                Longitude = longitude,
                                Rating = rating,
                                Reviews = reviews,
                                Address = address
                            });
                        }
                    }
                    catch
                    {
                        // skip malformed item
                    }
                }
            }

            return Ok(list);
        }

        /// <summary>
        /// Get charging stations from Google Maps for Staff monitoring page
        /// </summary>
        [HttpGet("charging-stations")]
        public async Task<IActionResult> GetChargingStations(
            [FromQuery] string location = "Ho Chi Minh City, Vietnam",
            [FromQuery] double? lat = null,
            [FromQuery] double? lng = null,
            [FromQuery] int zoom = 12)
        {
            var apiKey = _configuration["SerpApi:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return StatusCode(500, "SerpApi key is not configured");
            }

            var client = _httpClientFactory.CreateClient();
            
            // Use provided coordinates or default to Ho Chi Minh City
            var latitude = lat ?? 10.8231;
            var longitude = lng ?? 106.6297;
            var ll = $"@{latitude},{longitude},{zoom}z";
            
            var query = "EV charging station";
            var url = $"https://serpapi.com/search.json?engine=google_maps&q={Uri.EscapeDataString(query)}&ll={Uri.EscapeDataString(ll)}&api_key={Uri.EscapeDataString(apiKey)}";

            try
            {
                using var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "Failed to fetch from SerpApi");
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var stations = new List<object>();
                
                if (root.TryGetProperty("local_results", out var localResults) && localResults.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in localResults.EnumerateArray())
                    {
                        try
                        {
                            var station = ParseChargingStation(item);
                            if (station != null)
                            {
                                stations.Add(station);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[MapsController] Error parsing station: {ex.Message}");
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    count = stations.Count,
                    source = "Google Maps (SerpApi)",
                    location = location,
                    stations = stations
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapsController] Error fetching stations: {ex.Message}");
                return StatusCode(500, new { error = "Failed to fetch charging stations", details = ex.Message });
            }
        }

        private object? ParseChargingStation(JsonElement item)
        {
            // Get basic info
            string? title = item.TryGetProperty("title", out var t) ? t.GetString() : null;
            string? address = item.TryGetProperty("address", out var a) ? a.GetString() : null;
            
            if (string.IsNullOrWhiteSpace(title))
                return null;

            // Get GPS coordinates
            double latitude = 0;
            double longitude = 0;
            if (item.TryGetProperty("gps_coordinates", out var gps))
            {
                if (gps.TryGetProperty("latitude", out var la))
                    latitude = la.GetDouble();
                if (gps.TryGetProperty("longitude", out var lo))
                    longitude = lo.GetDouble();
            }

            if (latitude == 0 || longitude == 0)
                return null;

            // Get rating and reviews
            double? rating = item.TryGetProperty("rating", out var r) && r.ValueKind == JsonValueKind.Number 
                ? r.GetDouble() : null;
            int? reviews = item.TryGetProperty("reviews", out var rv) && rv.ValueKind == JsonValueKind.Number 
                ? rv.GetInt32() : null;

            // Get phone
            string? phone = item.TryGetProperty("phone", out var p) ? p.GetString() : null;

            // Get operating hours
            string? hours = null;
            bool is24Hours = false;
            if (item.TryGetProperty("hours", out var h) && h.ValueKind == JsonValueKind.String)
            {
                hours = h.GetString();
                is24Hours = hours?.Contains("Open 24 hours", StringComparison.OrdinalIgnoreCase) ?? false;
            }

            // Get type/service options
            string? type = item.TryGetProperty("type", out var tp) ? tp.GetString() : null;
            
            // Parse address to get city info
            var addressParts = address?.Split(',') ?? Array.Empty<string>();
            string? city = addressParts.Length > 1 ? addressParts[^2].Trim() : null;
            string? province = addressParts.Length > 0 ? addressParts[^1].Trim() : null;

            // Create mock spots based on typical charging station setup
            var spots = GenerateMockSpots(title);

            return new
            {
                id = Guid.NewGuid(),
                name = title,
                address = address,
                city = city ?? "Thành phố Hồ Chí Minh",
                province = province ?? "Vietnam",
                latitude = latitude,
                longitude = longitude,
                phone = phone,
                email = $"info@{title?.Replace(" ", "").ToLower()}.vn",
                status = 0, // Active
                description = $"Trạm sạc {title}",
                rating = rating,
                reviews = reviews,
                is24Hours = is24Hours,
                openingTime = is24Hours ? null : "07:00",
                closingTime = is24Hours ? null : "22:00",
                hours = hours,
                type = type,
                chargingSpots = spots,
                source = "Google Maps"
            };
        }

        private List<object> GenerateMockSpots(string? stationName)
        {
            // Generate 2-6 spots randomly based on station
            var random = new Random(stationName?.GetHashCode() ?? 0);
            var spotCount = random.Next(2, 7);
            var spots = new List<object>();

            var connectorTypes = new[] { "Type 2", "CCS", "CHAdeMO", "Type 2" };
            var powers = new[] { 22, 50, 100, 150 };
            var prices = new[] { 3000, 3500, 4000, 4500 };

            for (int i = 0; i < spotCount; i++)
            {
                var connectorIndex = i % connectorTypes.Length;
                var available = random.Next(0, 100) > 30; // 70% available

                spots.Add(new
                {
                    id = Guid.NewGuid(),
                    spotNumber = $"A{(i + 1):D2}",
                    status = available ? 0 : 1, // 0=Available, 1=Occupied
                    connectorType = connectorTypes[connectorIndex],
                    powerOutput = powers[connectorIndex],
                    pricePerKwh = prices[connectorIndex],
                    description = $"{connectorTypes[connectorIndex]} charging point"
                });
            }

            return spots;
        }
    }
}

