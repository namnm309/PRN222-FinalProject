using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
    [Table("tbl_user")]
    public class Users : BaseEntity
    {

        public string FullName { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string Gender { get; set; }

        //acc
        public bool IsActive { get; set; } = false;

        public UserRole Role { get; set; } = UserRole.EVDriver;

        public string? GoogleId { get; set; }

        // Navigation properties
        public virtual ICollection<UserVehicle> UserVehicles { get; set; } = new List<UserVehicle>();

        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        public virtual ICollection<ChargingSession> ChargingSessions { get; set; } = new List<ChargingSession>();

        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
