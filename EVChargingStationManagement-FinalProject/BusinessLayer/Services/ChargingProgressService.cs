using BusinessLayer.DTOs;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class ChargingProgressService : IChargingProgressService
    {
        private readonly EVDbContext _context;
        private readonly IRealtimeNotifier? _notifier;

        public ChargingProgressService(EVDbContext context)
        {
            _context = context;
            _notifier = null; // Will be set via property injection or separate method if needed
        }

        // Constructor with notifier for dependency injection
        public ChargingProgressService(EVDbContext context, IRealtimeNotifier notifier)
        {
            _context = context;
            _notifier = notifier;
        }

        public async Task<ChargingProgressDTO?> GetProgressAsync(Guid sessionId)
        {
            var session = await _context.ChargingSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                return null;

            return new ChargingProgressDTO
            {
                SessionId = session.Id,
                CurrentSocPercentage = session.CurrentSocPercentage,
                InitialSocPercentage = session.InitialSocPercentage,
                TargetSocPercentage = session.TargetSocPercentage,
                CurrentPowerKw = session.CurrentPowerKw,
                EnergyDeliveredKwh = session.EnergyDeliveredKwh,
                LastUpdatedAt = session.LastUpdatedAt,
                Status = session.Status.ToString(),
                EstimatedTimeRemainingMinutes = CalculateEstimatedTimeRemaining(session)
            };
        }

        public async Task UpdateProgressAsync(Guid sessionId, UpdateChargingProgressRequest request)
        {
            var session = await _context.ChargingSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                throw new ArgumentException("Session not found", nameof(sessionId));

            session.CurrentSocPercentage = request.SocPercentage;
            session.CurrentPowerKw = request.PowerKw;
            session.EnergyDeliveredKwh = request.EnergyDeliveredKwh;
            session.LastUpdatedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            // Save progress history
            var progress = new ChargingSessionProgress
            {
                Id = Guid.NewGuid(),
                ChargingSessionId = sessionId,
                RecordedAt = DateTime.UtcNow,
                SocPercentage = request.SocPercentage,
                PowerKw = request.PowerKw,
                EnergyDeliveredKwh = request.EnergyDeliveredKwh,
                EstimatedTimeRemainingMinutes = request.EstimatedTimeRemainingMinutes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ChargingSessionProgresses.Add(progress);
            await _context.SaveChangesAsync();

            // Notify progress update via SignalR
            if (_notifier != null)
            {
                var progressDto = new ChargingProgressDTO
                {
                    SessionId = sessionId,
                    CurrentSocPercentage = request.SocPercentage,
                    InitialSocPercentage = session.InitialSocPercentage,
                    TargetSocPercentage = session.TargetSocPercentage,
                    CurrentPowerKw = request.PowerKw,
                    EnergyDeliveredKwh = request.EnergyDeliveredKwh,
                    EstimatedTimeRemainingMinutes = request.EstimatedTimeRemainingMinutes,
                    LastUpdatedAt = DateTime.UtcNow,
                    Status = session.Status.ToString()
                };
                await _notifier.NotifyChargingProgressUpdatedAsync(sessionId, progressDto);
            }
        }

        public async Task<List<ChargingProgressDTO>> GetProgressHistoryAsync(Guid sessionId)
        {
            var progressHistory = await _context.ChargingSessionProgresses
                .Where(p => p.ChargingSessionId == sessionId)
                .OrderBy(p => p.RecordedAt)
                .ToListAsync();

            return progressHistory.Select(p => new ChargingProgressDTO
            {
                SessionId = sessionId,
                CurrentSocPercentage = p.SocPercentage,
                CurrentPowerKw = p.PowerKw,
                EnergyDeliveredKwh = p.EnergyDeliveredKwh,
                EstimatedTimeRemainingMinutes = p.EstimatedTimeRemainingMinutes,
                LastUpdatedAt = p.RecordedAt
            }).ToList();
        }

        private decimal? CalculateEstimatedTimeRemaining(ChargingSession session)
        {
            if (!session.CurrentSocPercentage.HasValue || 
                !session.TargetSocPercentage.HasValue || 
                !session.CurrentPowerKw.HasValue ||
                session.CurrentPowerKw.Value <= 0)
                return null;

            var socRemaining = session.TargetSocPercentage.Value - session.CurrentSocPercentage.Value;
            if (socRemaining <= 0)
                return 0;

            // Simplified calculation - in reality, this depends on battery capacity and charging curve
            // Assuming average charging efficiency
            var estimatedMinutes = (socRemaining / 100m) * 60m / session.CurrentPowerKw.Value;
            return estimatedMinutes;
        }
    }
}

