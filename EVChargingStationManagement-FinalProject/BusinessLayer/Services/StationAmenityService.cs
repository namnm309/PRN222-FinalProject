using System.Linq;
using BusinessLayer.DTOs;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class StationAmenityService : IStationAmenityService
    {
        private readonly EVDbContext _context;

        public StationAmenityService(EVDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StationAmenity>> GetAmenitiesByStationAsync(Guid stationId)
        {
            return await _context.StationAmenities
                .Where(a => a.ChargingStationId == stationId)
                .OrderBy(a => a.DisplayOrder)
                .ToListAsync();
        }

        public async Task<StationAmenity?> GetAmenityByIdAsync(Guid id)
        {
            return await _context.StationAmenities.FindAsync(id);
        }

        public async Task<StationAmenity> CreateAmenityAsync(CreateStationAmenityRequest request)
        {
            var amenity = new StationAmenity
            {
                Id = Guid.NewGuid(),
                ChargingStationId = request.ChargingStationId,
                Name = request.Name,
                Description = request.Description,
                IsActive = request.IsActive,
                DisplayOrder = request.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.StationAmenities.Add(amenity);
            await _context.SaveChangesAsync();
            return amenity;
        }

        public async Task<StationAmenity?> UpdateAmenityAsync(Guid id, UpdateStationAmenityRequest request)
        {
            var existingAmenity = await _context.StationAmenities.FindAsync(id);
            if (existingAmenity == null)
            {
                return null;
            }

            existingAmenity.Name = request.Name;
            existingAmenity.Description = request.Description;
            existingAmenity.IsActive = request.IsActive;
            existingAmenity.DisplayOrder = request.DisplayOrder;
            existingAmenity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingAmenity;
        }

        public async Task<bool> DeleteAmenityAsync(Guid id)
        {
            var amenity = await _context.StationAmenities.FindAsync(id);
            if (amenity == null)
            {
                return false;
            }

            _context.StationAmenities.Remove(amenity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

