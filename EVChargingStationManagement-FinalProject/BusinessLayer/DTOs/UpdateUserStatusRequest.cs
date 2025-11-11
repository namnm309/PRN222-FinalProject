using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs
{
    public class UpdateUserStatusRequest
    {
        [Required(ErrorMessage = "IsActive is required")]
        public bool IsActive { get; set; }
    }
}

