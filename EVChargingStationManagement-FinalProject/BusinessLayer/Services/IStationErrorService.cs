using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IStationErrorService
    {
        Task<IEnumerable<StationError>> GetAllErrorsAsync();
        Task<StationError?> GetErrorByIdAsync(Guid id);
        Task<IEnumerable<StationError>> GetErrorsByStationIdAsync(Guid stationId);
        Task<IEnumerable<StationError>> GetErrorsBySpotIdAsync(Guid spotId);
        Task<IEnumerable<StationError>> GetErrorsByStatusAsync(ErrorStatus status);
        Task<IEnumerable<StationError>> GetErrorsByUserIdAsync(Guid userId);
        Task<StationError> CreateErrorAsync(StationError error);
        Task<StationError?> UpdateErrorAsync(Guid id, StationError error);
        Task<bool> DeleteErrorAsync(Guid id);
        Task<bool> ErrorExistsAsync(Guid id);
    }
}

