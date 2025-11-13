using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Driver.Payment
{
    // AllowAnonymous: VNPay redirects here from external site, user may not be authenticated
    // Security: Payment validation is done via VNPay signature in the API endpoint
    [AllowAnonymous]
    public class VnPayReturnModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public Guid? PaymentId { get; set; }

        public void OnGet()
        {
            // Log for debugging
            Console.WriteLine($"[VnPayReturn] Page accessed. PaymentId: {PaymentId}");
            Console.WriteLine($"[VnPayReturn] Query string: {Request.QueryString}");
        }
    }
}

