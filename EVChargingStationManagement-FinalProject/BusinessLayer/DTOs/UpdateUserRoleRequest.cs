using System.ComponentModel.DataAnnotations;
using DataAccessLayer.Enums;

namespace BusinessLayer.DTOs
{
    public class UpdateUserRoleRequest
    {
        [Required(ErrorMessage = "Role is required")]
        public UserRole Role { get; set; }
    }
}

