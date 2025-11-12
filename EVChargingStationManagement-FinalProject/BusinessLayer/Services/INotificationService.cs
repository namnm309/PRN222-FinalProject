using DataAccessLayer.Entities;

namespace BusinessLayer.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<Notification>> GetNotificationsForUserAsync(Guid userId, bool unreadOnly = false);
        Task<Notification?> GetNotificationByIdAsync(Guid id);
        Task<Notification> CreateNotificationAsync(Notification notification);
        Task MarkAsReadAsync(Guid notificationId);
        Task MarkAllAsReadAsync(Guid userId);
    }
}

