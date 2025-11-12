using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
    [Table("tbl_notification")]
    public class Notification : BaseEntity
    {
        public Guid UserId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; } = NotificationType.General;

        public bool IsRead { get; set; }

        public DateTime SentAt { get; set; }

        public DateTime? ReadAt { get; set; }

        public string? ReferenceId { get; set; }

        public string? Metadata { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual Users? User { get; set; }
    }
}

