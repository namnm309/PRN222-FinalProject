using System.Linq;
using BusinessLayer.DTOs;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class NotificationService : INotificationService
    {
        private readonly EVDbContext _context;

        public NotificationService(EVDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<NotificationDTO>> GetNotificationsForUserAsync(Guid userId, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var notifications = await query
                .OrderByDescending(n => n.SentAt)
                .ToListAsync();
            
            return notifications.Select(MapToDTO);
        }

        public async Task<NotificationDTO?> GetNotificationByIdAsync(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            return notification == null ? null : MapToDTO(notification);
        }

        public async Task<NotificationDTO> CreateNotificationAsync(NotificationDTO notificationDto)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = notificationDto.UserId,
                Title = notificationDto.Title,
                Message = notificationDto.Message,
                Type = notificationDto.Type,
                IsRead = notificationDto.IsRead,
                ReferenceId = notificationDto.ReferenceId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SentAt = notificationDto.SentAt == default ? DateTime.UtcNow : notificationDto.SentAt
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            
            return MapToDTO(notification);
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
            {
                return;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                notification.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                notification.UpdatedAt = DateTime.UtcNow;
            }

            if (notifications.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
        }

        private NotificationDTO MapToDTO(Notification notification)
        {
            return new NotificationDTO
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                IsRead = notification.IsRead,
                SentAt = notification.SentAt,
                ReadAt = notification.ReadAt,
                ReferenceId = notification.ReferenceId
            };
        }
    }
}

