using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("tbl_refresh_token")]
    public class RefreshToken : BaseEntity
    {
        public string Token { get; set; } = string.Empty;

        public Guid UserId { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool IsRevoked { get; set; } = false;

        public DateTime? RevokedAt { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public virtual Users? User { get; set; }
    }
}





