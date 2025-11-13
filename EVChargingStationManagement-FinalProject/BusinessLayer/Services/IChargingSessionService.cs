using BusinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IChargingSessionService
    {
        Task<IEnumerable<ChargingSession>> GetSessionsForUserAsync(Guid userId, int limit = 20);
        Task<IEnumerable<ChargingSession>> GetActiveSessionsAsync(Guid? stationId = null);
        Task<ChargingSession?> GetActiveSessionForUserAsync(Guid userId);
        Task<ChargingSession?> GetSessionByIdAsync(Guid id);
        Task<ChargingSession> StartSessionAsync(Guid userId, StartChargingSessionRequest request);
        Task<ChargingSession?> CompleteSessionAsync(Guid sessionId, decimal energyDeliveredKwh, decimal cost, decimal? pricePerKwh, string? notes);
        Task<ChargingSession?> UpdateSessionStatusAsync(Guid sessionId, ChargingSessionStatus status, string? notes = null);
    }
}

