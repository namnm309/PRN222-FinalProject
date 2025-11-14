using BusinessLayer.DTOs;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IChargingSessionService
    {
        Task<IEnumerable<ChargingSessionDTO>> GetSessionsForUserAsync(Guid userId, int limit = 20);
        Task<IEnumerable<ChargingSessionDTO>> GetActiveSessionsAsync(Guid? stationId = null);
        Task<ChargingSessionDTO?> GetActiveSessionForUserAsync(Guid userId);
        Task<ChargingSessionDTO?> GetSessionByIdAsync(Guid id);
        Task<ChargingSessionDTO> StartSessionAsync(Guid userId, StartChargingSessionRequest request);
        Task<ChargingSessionDTO?> CompleteSessionAsync(Guid sessionId, decimal energyDeliveredKwh, decimal cost, decimal? pricePerKwh, string? notes);
        Task<ChargingSessionDTO?> UpdateSessionStatusAsync(Guid sessionId, ChargingSessionStatus status, string? notes = null);
    }
}

