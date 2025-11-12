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

        public async Task<IEnumerable<ChargingStation>> GetAllStationsAsync()
        {
            return await _context.ChargingStations
                .Include(s => s.ChargingSpots)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<ChargingStation?> GetStationByIdAsync(Guid id)
        {
            return await _context.ChargingStations
                .Include(s => s.ChargingSpots)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<ChargingStation>> GetStationsByStatusAsync(StationStatus status)
        {
            return await _context.ChargingStations
                .Include(s => s.ChargingSpots)
                .Where(s => s.Status == status)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChargingStation>> GetNearestStationsAsync(decimal latitude, decimal longitude, double radiusKm = 10, StationStatus? status = null, string? connectorType = null)
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

            return stationsWithDistance;
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

        public async Task<ChargingStation> CreateStationAsync(ChargingStation station)
        {
            if (station == null)
                throw new ArgumentNullException(nameof(station));

            station.Id = Guid.NewGuid();
            station.CreatedAt = DateTime.UtcNow;
            station.UpdatedAt = DateTime.UtcNow;

            _context.ChargingStations.Add(station);
            await _context.SaveChangesAsync();

            return station;
        }

        public async Task<ChargingStation?> UpdateStationAsync(Guid id, ChargingStation station)
        {
            if (station == null)
                throw new ArgumentNullException(nameof(station));

            var existingStation = await _context.ChargingStations.FindAsync(id);
            if (existingStation == null)
                return null;

            existingStation.Name = station.Name;
            existingStation.Address = station.Address;
            existingStation.City = station.City;
            existingStation.Province = station.Province;
            existingStation.PostalCode = station.PostalCode;
            existingStation.Latitude = station.Latitude;
            existingStation.Longitude = station.Longitude;
            existingStation.Phone = station.Phone;
            existingStation.Email = station.Email;
            existingStation.Status = station.Status;
            existingStation.Description = station.Description;
            existingStation.OpeningTime = station.OpeningTime;
            existingStation.ClosingTime = station.ClosingTime;
            existingStation.Is24Hours = station.Is24Hours;
            existingStation.SerpApiPlaceId = station.SerpApiPlaceId;
            existingStation.ExternalRating = station.ExternalRating;
            existingStation.ExternalReviewCount = station.ExternalReviewCount;
            existingStation.IsFromSerpApi = !string.IsNullOrWhiteSpace(station.SerpApiPlaceId);
            existingStation.SerpApiLastSynced = !string.IsNullOrWhiteSpace(station.SerpApiPlaceId) ? DateTime.UtcNow : existingStation.SerpApiLastSynced;
            existingStation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return existingStation;
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
    }
}

