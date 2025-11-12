using BusinessLayer.DTOs;

namespace BusinessLayer.Services
{
    public interface IChargingProgressService
    {
        Task<ChargingProgressDTO?> GetProgressAsync(Guid sessionId);
        Task UpdateProgressAsync(Guid sessionId, UpdateChargingProgressRequest request);
        Task<List<ChargingProgressDTO>> GetProgressHistoryAsync(Guid sessionId);
    }
}

