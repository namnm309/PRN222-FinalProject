using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs
{
    public class GoogleLoginRequest
    {
        [Required(ErrorMessage = "Google ID token is required")]
        public string IdToken { get; set; } = string.Empty;
    }
}


