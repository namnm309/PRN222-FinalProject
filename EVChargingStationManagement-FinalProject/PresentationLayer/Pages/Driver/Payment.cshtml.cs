using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Driver
{
    [Authorize(Roles = "EVDriver,Admin")]
    public class PaymentModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public Guid? SessionId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? PaymentId { get; set; }

        public void OnGet()
        {
        }
    }
}

