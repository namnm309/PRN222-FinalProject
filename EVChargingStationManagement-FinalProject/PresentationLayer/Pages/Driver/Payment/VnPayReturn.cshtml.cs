using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Driver.Payment
{
    [Authorize(Roles = "EVDriver,Admin")]
    public class VnPayReturnModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public Guid? PaymentId { get; set; }

        public void OnGet()
        {
        }
    }
}

