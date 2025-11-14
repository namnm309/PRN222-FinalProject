using DataAccessLayer.Enums;

namespace BusinessLayer.DTOs
{
    public class NotificationDTO
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? ReferenceId { get; set; }
    }
}

