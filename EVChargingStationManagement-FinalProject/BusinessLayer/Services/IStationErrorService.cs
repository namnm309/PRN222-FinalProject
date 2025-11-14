using BusinessLayer.DTOs;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IStationErrorService
    {
        Task<IEnumerable<StationErrorDTO>> GetAllErrorsAsync();
        Task<StationErrorDTO?> GetErrorByIdAsync(Guid id);
        Task<IEnumerable<StationErrorDTO>> GetErrorsByStationIdAsync(Guid stationId);
        Task<IEnumerable<StationErrorDTO>> GetErrorsBySpotIdAsync(Guid spotId);
        Task<IEnumerable<StationErrorDTO>> GetErrorsByStatusAsync(ErrorStatus status);
        Task<IEnumerable<StationErrorDTO>> GetErrorsByStatusStringAsync(string status);
        Task<IEnumerable<StationErrorDTO>> GetErrorsByUserIdAsync(Guid userId);
        Task<StationErrorDTO> CreateErrorAsync(CreateStationErrorRequest request);
        Task<StationErrorDTO?> UpdateErrorAsync(Guid id, UpdateStationErrorRequest request);
        Task<bool> DeleteErrorAsync(Guid id);
        Task<bool> ErrorExistsAsync(Guid id);
    }
}

