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

        public async Task<IEnumerable<StationAmenityDTO>> GetAmenitiesByStationAsync(Guid stationId)
        {
            var amenities = await _context.StationAmenities
                .Where(a => a.ChargingStationId == stationId)
                .OrderBy(a => a.DisplayOrder)
                .ToListAsync();
            
            return amenities.Select(MapToDTO);
        }

        public async Task<StationAmenityDTO?> GetAmenityByIdAsync(Guid id)
        {
            var amenity = await _context.StationAmenities.FindAsync(id);
            return amenity == null ? null : MapToDTO(amenity);
        }

        public async Task<StationAmenityDTO> CreateAmenityAsync(CreateStationAmenityRequest request)
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
            
            var createdAmenity = await _context.StationAmenities.FindAsync(amenity.Id);
            return MapToDTO(createdAmenity!);
        }

        public async Task<StationAmenityDTO?> UpdateAmenityAsync(Guid id, UpdateStationAmenityRequest request)
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
            
            var updatedAmenity = await _context.StationAmenities.FindAsync(id);
            return updatedAmenity == null ? null : MapToDTO(updatedAmenity);
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

        private StationAmenityDTO MapToDTO(StationAmenity amenity)
        {
            return new StationAmenityDTO
            {
                Id = amenity.Id,
                ChargingStationId = amenity.ChargingStationId,
                Name = amenity.Name,
                Description = amenity.Description,
                IsActive = amenity.IsActive,
                DisplayOrder = amenity.DisplayOrder
            };
        }
    }
}

