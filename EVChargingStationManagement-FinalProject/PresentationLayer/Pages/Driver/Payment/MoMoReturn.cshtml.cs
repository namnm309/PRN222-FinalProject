using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Driver.Payment
{
    // AllowAnonymous: MoMo redirects here from external site, user may not be authenticated
    // Security: Payment validation is done via MoMo signature in the API endpoint
    [AllowAnonymous]
    public class MoMoReturnModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public Guid? PaymentId { get; set; }

        public void OnGet()
        {
            // Log for debugging
            Console.WriteLine($"[MoMoReturn] Page accessed. PaymentId: {PaymentId}");
            Console.WriteLine($"[MoMoReturn] Query string: {Request.QueryString}");
        }
    }
}

