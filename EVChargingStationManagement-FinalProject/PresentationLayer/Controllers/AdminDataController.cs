using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace PresentationLayer.Controllers
{
	[ApiController]
	[Route("api/admin/data")]
	[Authorize(Roles = "Admin")]
	public class AdminDataController : ControllerBase
	{
		private readonly IConfiguration _configuration;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly EVDbContext _db;

		public AdminDataController(IConfiguration configuration, IHttpClientFactory httpClientFactory, EVDbContext db)
		{
			_configuration = configuration;
			_httpClientFactory = httpClientFactory;
			_db = db;
		}

		public class ImportResult
		{
			public int CreatedStations { get; set; }
			public int UpdatedStations { get; set; }
			public int CreatedSpots { get; set; }
		}

		[HttpPost("import-serp")]
		public async Task<IActionResult> ImportFromSerpAsync([FromQuery] string query, [FromQuery] double lat, [FromQuery] double lng, [FromQuery] int zoom = 14)
		{
			if (string.IsNullOrWhiteSpace(query)) query = "EV charging station";
			var apiKey = _configuration["SerpApi:ApiKey"];
			if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_SERPAPI_KEY_HERE")
				return StatusCode(500, "SerpApi key is not configured");

			var client = _httpClientFactory.CreateClient();
			var ll = $"@{lat},{lng},{zoom}z";
			var url = $"https://serpapi.com/search.json?engine=google_maps&q={Uri.EscapeDataString(query)}&ll={Uri.EscapeDataString(ll)}&api_key={Uri.EscapeDataString(apiKey)}";

			using var response = await client.GetAsync(url);
			if (!response.IsSuccessStatusCode)
				return StatusCode((int)response.StatusCode, "Failed to fetch from SerpApi");

			var json = await response.Content.ReadAsStringAsync();
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;

			int createdStations = 0, updatedStations = 0, createdSpots = 0;

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

						double latitude = 0, longitude = 0;
						if (item.TryGetProperty("gps_coordinates", out var gps) && gps.ValueKind == JsonValueKind.Object)
						{
							if (gps.TryGetProperty("latitude", out var la) && la.ValueKind == JsonValueKind.Number) latitude = la.GetDouble();
							if (gps.TryGetProperty("longitude", out var lo) && lo.ValueKind == JsonValueKind.Number) longitude = lo.GetDouble();
						}

					if (string.IsNullOrWhiteSpace(title) || latitude == 0 || longitude == 0)
						continue;

					// Check for duplicate: same Name AND Address, or very close coordinates (within 50 meters)
					var latDecimal = (decimal)latitude;
					var lngDecimal = (decimal)longitude;
					var existing = await _db.ChargingStations
						.FirstOrDefaultAsync(s => 
							(s.Name == title && (!string.IsNullOrWhiteSpace(address) && s.Address == address))
							|| (s.Latitude.HasValue && s.Longitude.HasValue 
								&& Math.Abs((double)(s.Latitude.Value - latDecimal)) < 0.0005 // ~50 meters
								&& Math.Abs((double)(s.Longitude.Value - lngDecimal)) < 0.0005));
					
					if (existing == null)
						{
							var station = new ChargingStation
							{
								Id = Guid.NewGuid(),
								Name = title,
								Address = address ?? string.Empty,
								Latitude = (decimal?)latitude,
								Longitude = (decimal?)longitude,
								Status = StationStatus.Active,
								CreatedAt = DateTime.UtcNow,
								UpdatedAt = DateTime.UtcNow
							};
							_db.ChargingStations.Add(station);
							await _db.SaveChangesAsync();
							createdStations++;

							// Create a few default spots for visibility (can be edited later)
							for (int i = 1; i <= 4; i++)
							{
								_db.ChargingSpots.Add(new ChargingSpot
								{
									Id = Guid.NewGuid(),
									ChargingStationId = station.Id,
									SpotNumber = i.ToString(),
									Status = SpotStatus.Available,
									CreatedAt = DateTime.UtcNow,
									UpdatedAt = DateTime.UtcNow
								});
								createdSpots++;
							}
							await _db.SaveChangesAsync();
						}
						else
						{
							existing.Address = address ?? existing.Address;
							existing.Latitude = (decimal?)latitude;
							existing.Longitude = (decimal?)longitude;
							existing.UpdatedAt = DateTime.UtcNow;
							await _db.SaveChangesAsync();
							updatedStations++;
						}
					}
					catch
					{
						// ignore malformed item
					}
				}
			}

			return Ok(new ImportResult { CreatedStations = createdStations, UpdatedStations = updatedStations, CreatedSpots = createdSpots });
		}
	}
}
