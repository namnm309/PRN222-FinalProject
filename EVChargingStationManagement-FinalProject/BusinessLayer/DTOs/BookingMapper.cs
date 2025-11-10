using BusinessLayer.DTOs;
using DataAccessLayer.Entities;

namespace BusinessLayer.DTOs
{
	public static class BookingMapper
	{
		public static BookingDTO ToDTO(this Booking entity)
		{
			return new BookingDTO
			{
				Id = entity.Id,
				UserId = entity.UserId,
				VehicleId = entity.VehicleId,
				ChargingStationId = entity.ChargingStationId,
				ChargingSpotId = entity.ChargingSpotId,
				StartTime = entity.StartTime,
				EndTime = entity.EndTime,
				Status = entity.Status,
				Notes = entity.Notes,
				CreatedAt = entity.CreatedAt,
				UpdatedAt = entity.UpdatedAt
			};
		}

		public static Booking ToEntity(this CreateBookingRequest request, Guid userId)
		{
			return new Booking
			{
				Id = Guid.NewGuid(),
				UserId = userId,
				VehicleId = request.VehicleId,
				ChargingStationId = request.ChargingStationId,
				ChargingSpotId = request.ChargingSpotId,
				StartTime = request.StartTime,
				EndTime = request.EndTime,
				Status = "Pending",
				Notes = request.Notes,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};
		}
	}
}


