using System.Linq;
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

        public async Task<IEnumerable<Notification>> GetNotificationsForUserAsync(Guid userId, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.SentAt)
                .ToListAsync();
        }

        public async Task<Notification?> GetNotificationByIdAsync(Guid id)
        {
            return await _context.Notifications.FindAsync(id);
        }

        public async Task<Notification> CreateNotificationAsync(Notification notification)
        {
            notification.Id = Guid.NewGuid();
            notification.CreatedAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;
            notification.SentAt = notification.SentAt == default ? DateTime.UtcNow : notification.SentAt;

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
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
    }
}

