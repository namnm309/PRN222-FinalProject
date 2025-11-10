using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IChargingSessionService
    {
        // Query methods
        Task<IEnumerable<ChargingSession>> GetAllSessionsAsync();
        Task<ChargingSession?> GetSessionByIdAsync(Guid id);
        Task<IEnumerable<ChargingSession>> GetSessionsByUserIdAsync(Guid userId);
        Task<IEnumerable<ChargingSession>> GetSessionsByStationIdAsync(Guid stationId);
        Task<IEnumerable<ChargingSession>> GetSessionsBySpotIdAsync(Guid spotId);
        Task<IEnumerable<ChargingSession>> GetSessionsByStatusAsync(SessionStatus status);
        Task<IEnumerable<ChargingSession>> GetActiveSessionsAsync();
        Task<ChargingSession?> GetActiveSessionBySpotIdAsync(Guid spotId);
        Task<ChargingSession?> GetActiveSessionByUserIdAsync(Guid userId);
        
        // CRUD methods
        Task<ChargingSession> CreateSessionAsync(ChargingSession session);
        Task<ChargingSession?> UpdateSessionAsync(Guid id, ChargingSession session);
        Task<ChargingSession?> StopSessionAsync(Guid id, decimal energyConsumed, decimal totalCost, string? paymentMethod, string? notes);
        Task<ChargingSession?> PauseSessionAsync(Guid id, string? notes);
        Task<ChargingSession?> ResumeSessionAsync(Guid id, string? notes);
        Task<ChargingSession?> CancelSessionAsync(Guid id, string? reason);
        Task<bool> DeleteSessionAsync(Guid id);
        
        // Validation
        Task<bool> SessionExistsAsync(Guid id);
        Task<bool> CanStartSessionAsync(Guid spotId);
    }
}

