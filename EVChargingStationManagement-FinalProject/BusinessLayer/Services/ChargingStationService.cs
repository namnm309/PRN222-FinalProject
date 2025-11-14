using BusinessLayer.DTOs;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class ChargingStationService : IChargingStationService
    {
        private readonly EVDbContext _context;

        public ChargingStationService(EVDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ChargingStationDTO>> GetAllStationsAsync()
        {
            var stations = await _context.ChargingStations
                .Include(s => s.ChargingSpots)
                .OrderBy(s => s.Name)
                .ToListAsync();
            
            return stations.Select(MapToDTO);
        }

        public async Task<ChargingStationDTO?> GetStationByIdAsync(Guid id)
        {
            var station = await _context.ChargingStations
                .Include(s => s.ChargingSpots)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            return station == null ? null : MapToDTO(station);
        }

        public async Task<IEnumerable<ChargingStationDTO>> GetStationsByStatusAsync(StationStatus status)
        {
            var stations = await _context.ChargingStations
                .Include(s => s.ChargingSpots)
                .Where(s => s.Status == status)
                .OrderBy(s => s.Name)
                .ToListAsync();
            
            return stations.Select(MapToDTO);
        }

        public async Task<IEnumerable<ChargingStationDTO>> GetNearestStationsAsync(decimal latitude, decimal longitude, double radiusKm = 10, StationStatus? status = null, string? connectorType = null)
        {
            var query = _context.ChargingStations
                .Include(s => s.ChargingSpots)
                .Where(s => s.Latitude.HasValue && s.Longitude.HasValue);

            // Filter by status if provided
            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            // Filter by connector type if provided
            if (!string.IsNullOrWhiteSpace(connectorType))
            {
                query = query.Where(s => s.ChargingSpots.Any(cs => cs.ConnectorType == connectorType));
            }

            var stations = await query.ToListAsync();

            // Calculate distance using Haversine formula and filter by radius
            var stationsWithDistance = stations
                .Select(s => new
                {
                    Station = s,
                    DistanceKm = CalculateDistanceKm(
                        (double)latitude,
                        (double)longitude,
                        (double)s.Latitude!.Value,
                        (double)s.Longitude!.Value
                    )
                })
                .Where(x => x.DistanceKm <= radiusKm)
                .OrderBy(x => x.DistanceKm)
                .Select(x => x.Station)
                .ToList();

            return stationsWithDistance.Select(MapToDTO);
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

        public async Task<ChargingStationDTO> CreateStationAsync(CreateChargingStationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var station = new ChargingStation
            {
                Id = Guid.NewGuid(),
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
                Is24Hours = request.Is24Hours,
                SerpApiPlaceId = request.SerpApiPlaceId,
                ExternalRating = request.ExternalRating,
                ExternalReviewCount = request.ExternalReviewCount,
                IsFromSerpApi = !string.IsNullOrWhiteSpace(request.SerpApiPlaceId),
                SerpApiLastSynced = !string.IsNullOrWhiteSpace(request.SerpApiPlaceId) ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ChargingStations.Add(station);
            await _context.SaveChangesAsync();

            // Reload với navigation properties
            var createdStation = await _context.ChargingStations
                .Include(s => s.ChargingSpots)
                .FirstOrDefaultAsync(s => s.Id == station.Id);
            
            return MapToDTO(createdStation!);
        }

        public async Task<ChargingStationDTO?> UpdateStationAsync(Guid id, UpdateChargingStationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var existingStation = await _context.ChargingStations.FindAsync(id);
            if (existingStation == null)
                return null;

            existingStation.Name = request.Name;
            existingStation.Address = request.Address;
            existingStation.City = request.City;
            existingStation.Province = request.Province;
            existingStation.PostalCode = request.PostalCode;
            existingStation.Latitude = request.Latitude;
            existingStation.Longitude = request.Longitude;
            existingStation.Phone = request.Phone;
            existingStation.Email = request.Email;
            existingStation.Status = request.Status;
            existingStation.Description = request.Description;
            existingStation.OpeningTime = request.OpeningTime;
            existingStation.ClosingTime = request.ClosingTime;
            existingStation.Is24Hours = request.Is24Hours;
            
            // Only update SerpApi fields if provided
            if (request.SerpApiPlaceId != null)
            {
                existingStation.SerpApiPlaceId = request.SerpApiPlaceId;
                existingStation.IsFromSerpApi = !string.IsNullOrWhiteSpace(request.SerpApiPlaceId);
                existingStation.SerpApiLastSynced = !string.IsNullOrWhiteSpace(request.SerpApiPlaceId) ? DateTime.UtcNow : existingStation.SerpApiLastSynced;
            }
            
            if (request.ExternalRating.HasValue)
                existingStation.ExternalRating = request.ExternalRating;
            
            if (request.ExternalReviewCount.HasValue)
                existingStation.ExternalReviewCount = request.ExternalReviewCount;
            
            existingStation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Reload với navigation properties
            var updatedStation = await _context.ChargingStations
                .Include(s => s.ChargingSpots)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            return updatedStation == null ? null : MapToDTO(updatedStation);
        }

        public async Task<bool> DeleteStationAsync(Guid id)
        {
            var station = await _context.ChargingStations.FindAsync(id);
            if (station == null)
                return false;

            _context.ChargingStations.Remove(station);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> StationExistsAsync(Guid id)
        {
            return await _context.ChargingStations.AnyAsync(s => s.Id == id);
        }

        private ChargingStationDTO MapToDTO(ChargingStation station)
        {
            var spots = station.ChargingSpots?.ToList() ?? new List<ChargingSpot>();
            // Lấy thông tin từ spot đầu tiên (hoặc spot có sẵn đầu tiên)
            var firstSpot = spots.FirstOrDefault(s => s.Status == SpotStatus.Available) ?? spots.FirstOrDefault();
            
            // Nếu station không Active, thì AvailableSpots = 0
            var availableSpots = station.Status == StationStatus.Active 
                ? spots.Count(s => s.Status == SpotStatus.Available)
                : 0;
            
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
                AvailableSpots = availableSpots,
                ConnectorType = firstSpot?.ConnectorType,
                PricePerKwh = firstSpot?.PricePerKwh,
                SerpApiPlaceId = station.SerpApiPlaceId,
                ExternalRating = station.ExternalRating,
                ExternalReviewCount = station.ExternalReviewCount,
                IsFromSerpApi = station.IsFromSerpApi,
                SerpApiLastSynced = station.SerpApiLastSynced
            };
        }
    }
}

