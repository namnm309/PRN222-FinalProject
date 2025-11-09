using BusinessLayer.Services;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PresentationLayer.DTOs;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChargingStationController : ControllerBase
    {
        private readonly IChargingStationService _stationService;
        private readonly IChargingSpotService _spotService;

        public ChargingStationController(
            IChargingStationService stationService,
            IChargingSpotService spotService)
        {
            _stationService = stationService;
            _spotService = spotService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStations()
        {
            var stations = await _stationService.GetAllStationsAsync();
            var stationDTOs = stations.Select(s => MapToDTO(s)).ToList();
            return Ok(stationDTOs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStationById(Guid id)
        {
            var station = await _stationService.GetStationByIdAsync(id);
            if (station == null)
                return NotFound(new { message = "Charging station not found" });

            return Ok(MapToDTO(station));
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetStationsByStatus(StationStatus status)
        {
            var stations = await _stationService.GetStationsByStatusAsync(status);
            var stationDTOs = stations.Select(s => MapToDTO(s)).ToList();
            return Ok(stationDTOs);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> CreateStation([FromBody] CreateChargingStationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var station = new ChargingStation
            {
                Name = request.Name,
                Address = request.Address,
                City = request.City,
                Province = request.Province,
                PostalCode = request.PostalCode,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Phone = request.Phone,
                Email = request.Email,
                Status = request.Status,
                Description = request.Description,
                OpeningTime = request.OpeningTime,
                ClosingTime = request.ClosingTime,
                Is24Hours = request.Is24Hours
            };

            var createdStation = await _stationService.CreateStationAsync(station);
            return CreatedAtAction(nameof(GetStationById), new { id = createdStation.Id }, MapToDTO(createdStation));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,CSStaff")]
        public async Task<IActionResult> UpdateStation(Guid id, [FromBody] UpdateChargingStationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var station = new ChargingStation
            {
                Name = request.Name,
                Address = request.Address,
                City = request.City,
                Province = request.Province,
                PostalCode = request.PostalCode,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Phone = request.Phone,
                Email = request.Email,
                Status = request.Status,
                Description = request.Description,
                OpeningTime = request.OpeningTime,
                ClosingTime = request.ClosingTime,
                Is24Hours = request.Is24Hours
            };

            var updatedStation = await _stationService.UpdateStationAsync(id, station);
            if (updatedStation == null)
                return NotFound(new { message = "Charging station not found" });

            return Ok(MapToDTO(updatedStation));
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

        private ChargingStationDTO MapToDTO(ChargingStation station)
        {
            var spots = station.ChargingSpots?.ToList() ?? new List<ChargingSpot>();
            return new ChargingStationDTO
            {
                Id = station.Id,
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
                CreatedAt = station.CreatedAt,
                UpdatedAt = station.UpdatedAt,
                TotalSpots = spots.Count,
                AvailableSpots = spots.Count(s => s.Status == SpotStatus.Available)
            };
        }
    }
}

