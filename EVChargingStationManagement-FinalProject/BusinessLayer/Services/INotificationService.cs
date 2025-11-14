using BusinessLayer.DTOs;

namespace BusinessLayer.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDTO>> GetNotificationsForUserAsync(Guid userId, bool unreadOnly = false);
        Task<NotificationDTO?> GetNotificationByIdAsync(Guid id);
        Task<NotificationDTO> CreateNotificationAsync(NotificationDTO notification);
        Task MarkAsReadAsync(Guid notificationId);
        Task MarkAllAsReadAsync(Guid userId);
    }
}

