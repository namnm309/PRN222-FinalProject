using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.DTOs;
using Microsoft.Extensions.Configuration;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChargingStationController : ControllerBase
    {
        private readonly IChargingStationService _stationService;
        private readonly IChargingSpotService _spotService;
        private readonly IStationDataMergeService _mergeService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IRealtimeNotifier _notifier;

        public ChargingStationController(
            IChargingStationService stationService,
            IChargingSpotService spotService,
            IStationDataMergeService mergeService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IRealtimeNotifier notifier)
        {
            _stationService = stationService;
            _spotService = spotService;
            _mergeService = mergeService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _notifier = notifier;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllStations()
        {
            var stations = await _stationService.GetAllStationsAsync();
            return Ok(stations);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStationById(Guid id)
        {
            var station = await _stationService.GetStationByIdAsync(id);
            if (station == null)
                return NotFound(new { message = "Charging station not found" });

            return Ok(station);
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetStationsByStatus([FromRoute] string status)
        {
            // Parse string to enum using reflection from DTO property type
            var statusPropertyType = typeof(ChargingStationDTO).GetProperty("Status")!.PropertyType;
            if (!Enum.TryParse(statusPropertyType, status, true, out var statusValue))
            {
                return BadRequest(new { message = "Invalid status value" });
            }
            
            // Call service method using reflection
            var method = typeof(IChargingStationService).GetMethod("GetStationsByStatusAsync");
            var task = (Task)method!.Invoke(_stationService, new[] { statusValue })!;
            await task;
            var resultProperty = task.GetType().GetProperty("Result");
            var stations = resultProperty!.GetValue(task);
            return Ok(stations);
        }

        [HttpGet("nearest")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNearestStations(
            [FromQuery] decimal lat,
            [FromQuery] decimal lng,
            [FromQuery] double radiusKm = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? connectorType = null)
        {
            // Parse status string to enum if provided
            object? statusEnum = null;
            if (!string.IsNullOrEmpty(status))
            {
                var statusPropertyType = typeof(ChargingStationDTO).GetProperty("Status")!.PropertyType;
                if (Enum.TryParse(statusPropertyType, status, true, out var parsedStatus))
                {
                    statusEnum = parsedStatus;
                }
            }
            
            // If no status provided, default to Active
            if (statusEnum == null)
            {
                var statusPropertyType = typeof(ChargingStationDTO).GetProperty("Status")!.PropertyType;
                statusEnum = Enum.Parse(statusPropertyType, "Active", true);
            }
            
            // Call service method using reflection
            var method = typeof(IChargingStationService).GetMethod("GetNearestStationsAsync");
            var task = (Task)method!.Invoke(_stationService, new object[] { lat, lng, radiusKm, statusEnum, connectorType! })!;
            await task;
            var resultProperty = task.GetType().GetProperty("Result");
            var stations = (IEnumerable<ChargingStationDTO>)resultProperty!.GetValue(task)!;
            
            // Calculate distance for each station
            var stationsWithDistance = stations.Select(s =>
            {
                s.DistanceKm = (decimal)CalculateDistanceKm(
                    (double)lat,
                    (double)lng,
                    (double)s.Latitude!.Value,
                    (double)s.Longitude!.Value
                );
                return s;
            }).ToList();

            return Ok(stationsWithDistance);
        }

        private double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth's radius in kilometers
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> CreateStation([FromBody] CreateChargingStationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createdStation = await _stationService.CreateStationAsync(request);

            // Tạo spots từ mảng chi tiết (nếu có) hoặc từ TotalSpots
            // Get SpotStatus.Available enum value using reflection
            var spotStatusType = typeof(CreateChargingSpotRequest).GetProperty("Status")!.PropertyType;
            var availableStatus = Enum.Parse(spotStatusType, "Available", true);
            
            if (request.Spots != null && request.Spots.Count > 0)
            {
                // Tạo từng spot chi tiết từ form admin
                foreach (var spotItem in request.Spots)
                {
                    var spotRequest = new CreateChargingSpotRequest
                    {
                        SpotNumber = spotItem.SpotNumber,
                        ChargingStationId = createdStation.Id,
                        Status = spotItem.Status,
                        ConnectorType = spotItem.ConnectorType,
                        PowerOutput = spotItem.PowerOutput,
                        PricePerKwh = spotItem.PricePerKwh,
                        Description = $"Spot {spotItem.SpotNumber}"
                    };

                    try
                    {
                        await _spotService.CreateSpotAsync(spotRequest);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating spot {spotItem.SpotNumber}: {ex.Message}");
                    }
                }
                
                // Notify spots list updated
                await _notifier.NotifySpotsListUpdatedAsync(createdStation.Id);
            }
            else if (request.TotalSpots > 0)
            {
                // Tạo tự động với giá trị mặc định (backward compatible)
                for (int i = 1; i <= request.TotalSpots; i++)
                {
                    var spotRequest = new CreateChargingSpotRequest
                    {
                        SpotNumber = i.ToString("D2"), // Format: 01, 02, 03, ...
                        ChargingStationId = createdStation.Id,
                        Status = (dynamic)availableStatus, // Use dynamic to set enum value
                        ConnectorType = request.DefaultConnectorType,
                        PowerOutput = request.DefaultPowerOutput,
                        PricePerKwh = request.DefaultPricePerKwh,
                        Description = $"Spot {i}"
                    };

                    try
                    {
                        await _spotService.CreateSpotAsync(spotRequest);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue creating other spots
                        Console.WriteLine($"Error creating spot {i}: {ex.Message}");
                    }
                }
                
                // Notify spots list updated
                await _notifier.NotifySpotsListUpdatedAsync(createdStation.Id);
            }

            return CreatedAtAction(nameof(GetStationById), new { id = createdStation.Id }, createdStation);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> UpdateStation(Guid id, [FromBody] UpdateChargingStationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updatedStation = await _stationService.UpdateStationAsync(id, request);
            if (updatedStation == null)
                return NotFound(new { message = "Charging station not found" });

            // Cập nhật spots nếu có yêu cầu
            if (request.DefaultConnectorType != null || request.DefaultPowerOutput.HasValue || request.DefaultPricePerKwh.HasValue)
            {
                // Cập nhật tất cả spots hiện có với giá trị mới
                var existingSpots = await _spotService.GetSpotsByStationIdAsync(id);
                foreach (var spot in existingSpots)
                {
                    var updateSpotRequest = new UpdateChargingSpotRequest
                    {
                        SpotNumber = spot.SpotNumber,
                        Status = spot.Status,
                        ConnectorType = request.DefaultConnectorType ?? spot.ConnectorType,
                        PowerOutput = request.DefaultPowerOutput ?? spot.PowerOutput,
                        PricePerKwh = request.DefaultPricePerKwh ?? spot.PricePerKwh,
                        Description = spot.Description
                    };

                    try
                    {
                        await _spotService.UpdateSpotAsync(spot.Id, updateSpotRequest);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating spot {spot.Id}: {ex.Message}");
                    }
                }
            }

            // Xử lý danh sách spots chi tiết nếu có (ưu tiên hơn TotalSpots)
            if (request.Spots != null && request.Spots.Any())
            {
                var existingSpots = await _spotService.GetSpotsByStationIdAsync(id);
                var existingSpotIds = existingSpots.Select(s => s.Id).ToList();
                var requestSpotIds = request.Spots.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToList();
                
                // Xóa các spots không có trong request (đã bị xóa ở client)
                var spotsToDelete = existingSpots.Where(s => !requestSpotIds.Contains(s.Id)).ToList();
                foreach (var spot in spotsToDelete)
                {
                    try
                    {
                        await _spotService.DeleteSpotAsync(spot.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting spot {spot.Id}: {ex.Message}");
                    }
                }
                
                // Tạo mới hoặc cập nhật spots
                foreach (var spotItem in request.Spots)
                {
                    try
                    {
                        if (spotItem.Id.HasValue && existingSpotIds.Contains(spotItem.Id.Value))
                        {
                            // Cập nhật spot hiện có
                            var updateSpotRequest = new UpdateChargingSpotRequest
                            {
                                SpotNumber = spotItem.SpotNumber,
                                Status = spotItem.Status,
                                ConnectorType = spotItem.ConnectorType,
                                PowerOutput = spotItem.PowerOutput,
                                PricePerKwh = spotItem.PricePerKwh,
                                Description = $"Spot {spotItem.SpotNumber}"
                            };
                            
                            await _spotService.UpdateSpotAsync(spotItem.Id.Value, updateSpotRequest);
                        }
                        else
                        {
                            // Tạo spot mới
                            var newSpotRequest = new CreateChargingSpotRequest
                            {
                                SpotNumber = spotItem.SpotNumber,
                                ChargingStationId = id,
                                Status = spotItem.Status,
                                ConnectorType = spotItem.ConnectorType,
                                PowerOutput = spotItem.PowerOutput,
                                PricePerKwh = spotItem.PricePerKwh,
                                Description = $"Spot {spotItem.SpotNumber}"
                            };
                            
                            await _spotService.CreateSpotAsync(newSpotRequest);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing spot {spotItem.SpotNumber}: {ex.Message}");
                    }
                }
                
                // Notify spots list updated
                await _notifier.NotifySpotsListUpdatedAsync(id);
            }
            // Thêm hoặc xóa spots nếu TotalSpots được chỉ định (chỉ khi không có Spots)
            else if (request.TotalSpots.HasValue && request.TotalSpots.Value > 0)
            {
                var existingSpots = await _spotService.GetSpotsByStationIdAsync(id);
                var currentCount = existingSpots.Count();
                var targetCount = request.TotalSpots.Value;

                if (targetCount > currentCount)
                {
                    // Thêm spots mới
                    var connectorType = request.DefaultConnectorType ?? existingSpots.FirstOrDefault()?.ConnectorType;
                    var powerOutput = request.DefaultPowerOutput ?? existingSpots.FirstOrDefault()?.PowerOutput;
                    var pricePerKwh = request.DefaultPricePerKwh ?? existingSpots.FirstOrDefault()?.PricePerKwh;

                    // Get SpotStatus.Available enum value using reflection
                    var spotStatusType = typeof(CreateChargingSpotRequest).GetProperty("Status")!.PropertyType;
                    var availableStatus = Enum.Parse(spotStatusType, "Available", true);
                    
                    for (int i = currentCount + 1; i <= targetCount; i++)
                    {
                        var newSpotRequest = new CreateChargingSpotRequest
                        {
                            SpotNumber = i.ToString("D2"),
                            ChargingStationId = id,
                            Status = (dynamic)availableStatus,
                            ConnectorType = connectorType,
                            PowerOutput = powerOutput,
                            PricePerKwh = pricePerKwh,
                            Description = $"Spot {i}"
                        };

                        try
                        {
                            await _spotService.CreateSpotAsync(newSpotRequest);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creating spot {i}: {ex.Message}");
                        }
                    }
                }
                else if (targetCount < currentCount)
                {
                    // Xóa spots thừa (xóa từ cuối lên)
                    var spotsToDelete = existingSpots.OrderByDescending(s => s.SpotNumber).Take(currentCount - targetCount);
                    foreach (var spot in spotsToDelete)
                    {
                        try
                        {
                            await _spotService.DeleteSpotAsync(spot.Id);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error deleting spot {spot.Id}: {ex.Message}");
                        }
                    }
                }
            }

            // Reload station với spots mới
            updatedStation = await _stationService.GetStationByIdAsync(id);
            if (updatedStation == null)
                return NotFound(new { message = "Charging station not found" });
            
            // Notify spots list updated if spots were modified (nếu chưa notify ở trên)
            if ((request.Spots == null || !request.Spots.Any()) && 
                (request.DefaultConnectorType != null || request.DefaultPowerOutput.HasValue || 
                request.DefaultPricePerKwh.HasValue || (request.TotalSpots.HasValue && request.TotalSpots.Value > 0)))
            {
                await _notifier.NotifySpotsListUpdatedAsync(id);
            }
            
            return Ok(updatedStation);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteStation(Guid id)
        {
            var result = await _stationService.DeleteStationAsync(id);
            if (!result)
                return NotFound(new { message = "Charging station not found" });

            return Ok(new { message = "Charging station deleted successfully" });
        }

        [HttpPatch("{id}/toggle-status")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> ToggleStationStatus(Guid id)
        {
            var station = await _stationService.GetStationByIdAsync(id);
            if (station == null)
                return NotFound(new { message = "Charging station not found" });

            // Toggle giữa Active và Inactive
            // Get enum values using reflection
            var statusType = typeof(ChargingStationDTO).GetProperty("Status")!.PropertyType;
            var activeStatus = Enum.Parse(statusType, "Active", true);
            var inactiveStatus = Enum.Parse(statusType, "Inactive", true);
            
            var currentStatusStr = station.Status.ToString();
            var newStatus = currentStatusStr == "Active" ? inactiveStatus : activeStatus;

            var updateRequest = new UpdateChargingStationRequest
            {
                Name = station.Name,
                Address = station.Address,
                City = station.City,
                Province = station.Province,
                PostalCode = station.PostalCode,
                Latitude = station.Latitude,
                Longitude = station.Longitude,
                Phone = station.Phone,
                Email = station.Email,
                Status = (dynamic)newStatus,
                Description = station.Description,
                OpeningTime = station.OpeningTime,
                ClosingTime = station.ClosingTime,
                Is24Hours = station.Is24Hours,
                SerpApiPlaceId = station.SerpApiPlaceId,
                ExternalRating = station.ExternalRating,
                ExternalReviewCount = station.ExternalReviewCount
            };

            var updatedStation = await _stationService.UpdateStationAsync(id, updateRequest);
            if (updatedStation == null)
                return NotFound(new { message = "Charging station not found" });

            // Gửi SignalR notification để cập nhật realtime cho tất cả clients đang xem trạm này
            await _notifier.NotifyStationStatusChangedAsync(
                updatedStation.Id, 
                updatedStation.Status.ToString(), 
                updatedStation.Name
            );

            return Ok(updatedStation);
        }

        [HttpPost("import-from-serpapi")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> ImportFromSerpApi([FromBody] SerpApiPlaceDTO serpPlace)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get StationStatus.Active enum value using reflection
            var statusType = typeof(CreateChargingStationRequest).GetProperty("Status")!.PropertyType;
            var activeStatus = Enum.Parse(statusType, "Active", true);

            var createRequest = new CreateChargingStationRequest
            {
                Name = serpPlace.Title,
                Address = serpPlace.Address ?? string.Empty,
                Latitude = (decimal)serpPlace.Latitude,
                Longitude = (decimal)serpPlace.Longitude,
                SerpApiPlaceId = serpPlace.PlaceId,
                ExternalRating = serpPlace.Rating.HasValue ? (decimal)serpPlace.Rating.Value : null,
                ExternalReviewCount = serpPlace.Reviews,
                Status = (dynamic)activeStatus
            };

            var created = await _stationService.CreateStationAsync(createRequest);
            return CreatedAtAction(nameof(GetStationById), new { id = created.Id }, created);
        }

        [HttpGet("merged")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMergedStations([FromQuery] string? query, [FromQuery] double? lat, [FromQuery] double? lng)
        {
            var dbStations = (await _stationService.GetAllStationsAsync())
                .ToList();

            List<SerpApiPlaceDTO> serpPlaces = new();
            
            if (!string.IsNullOrWhiteSpace(query) && lat.HasValue && lng.HasValue)
            {
                // Fetch from SerpApi
                var apiKey = _configuration["SerpApi:ApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey) && apiKey != "YOUR_SERPAPI_KEY_HERE")
                {
                    var client = _httpClientFactory.CreateClient();
                    var zoom = 14;
                    var ll = $"@{lat},{lng},{zoom}z";
                    var url = $"https://serpapi.com/search.json?engine=google_maps&q={Uri.EscapeDataString(query)}&ll={Uri.EscapeDataString(ll)}&api_key={Uri.EscapeDataString(apiKey)}";

                    try
                    {
                        using var response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            using var doc = System.Text.Json.JsonDocument.Parse(json);
                            var root = doc.RootElement;

                            if (root.TryGetProperty("local_results", out var localResults) && localResults.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var item in localResults.EnumerateArray())
                                {
                                    try
                                    {
                                        string? title = item.TryGetProperty("title", out var t) ? t.GetString() : null;
                                        string? address = item.TryGetProperty("address", out var a) ? a.GetString() : null;
                                        double? rating = item.TryGetProperty("rating", out var r) && r.ValueKind == System.Text.Json.JsonValueKind.Number ? r.GetDouble() : null;
                                        int? reviews = item.TryGetProperty("reviews", out var rv) && rv.ValueKind == System.Text.Json.JsonValueKind.Number ? rv.GetInt32() : null;
                                        string? placeId = item.TryGetProperty("place_id", out var pid) ? pid.GetString() : null;

                                        double latitude = 0;
                                        double longitude = 0;
                                        if (item.TryGetProperty("gps_coordinates", out var gps) && gps.ValueKind == System.Text.Json.JsonValueKind.Object)
                                        {
                                            if (gps.TryGetProperty("latitude", out var la) && la.ValueKind == System.Text.Json.JsonValueKind.Number)
                                                latitude = la.GetDouble();
                                            if (gps.TryGetProperty("longitude", out var lo) && lo.ValueKind == System.Text.Json.JsonValueKind.Number)
                                                longitude = lo.GetDouble();
                                        }

                                        if (!string.IsNullOrWhiteSpace(title) && latitude != 0 && longitude != 0)
                                        {
                                            serpPlaces.Add(new SerpApiPlaceDTO
                                            {
                                                PlaceId = placeId,
                                                Title = title,
                                                Address = address ?? "",
                                                Latitude = latitude,
                                                Longitude = longitude,
                                                Rating = rating,
                                                Reviews = reviews
                                            });
                                        }
                                    }
                                    catch
                                    {
                                        // Skip malformed items
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // If SerpApi fails, continue with DB data only
                    }
                }
            }

            var merged = await _mergeService.MergeSerpApiWithDatabaseAsync(serpPlaces, dbStations);
            return Ok(merged);
        }

        [HttpPut("{id}/link-serpapi")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> LinkWithSerpApi(Guid id, [FromBody] SerpApiPlaceDTO serpPlace)
        {
            var station = await _stationService.GetStationByIdAsync(id);
            if (station == null)
                return NotFound(new { message = "Station not found" });

            var updateRequest = new UpdateChargingStationRequest
            {
                Name = station.Name,
                Address = station.Address,
                City = station.City,
                Province = station.Province,
                PostalCode = station.PostalCode,
                Latitude = station.Latitude,
                Longitude = station.Longitude,
                Phone = station.Phone,
                Email = station.Email,
                Status = station.Status,
                Description = station.Description,
                OpeningTime = station.OpeningTime,
                ClosingTime = station.ClosingTime,
                Is24Hours = station.Is24Hours,
                SerpApiPlaceId = serpPlace.PlaceId,
                ExternalRating = serpPlace.Rating.HasValue ? (decimal)serpPlace.Rating.Value : null,
                ExternalReviewCount = serpPlace.Reviews
            };

            var updated = await _stationService.UpdateStationAsync(id, updateRequest);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }
    }
}

