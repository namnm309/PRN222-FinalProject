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
            public string PlaceId { get; set; }
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
                        string placeId = item.TryGetProperty("place_id", out var pid) ? pid.GetString() : null;

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
                                Address = address,
                                PlaceId = placeId
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
    }
}

