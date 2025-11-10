using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Staff
{
    [Authorize(Roles = "CSStaff,Admin")]
    public class StationDetailModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? StationId { get; set; }

        public void OnGet()
        {
            // StationId will be bound from route parameter
        }
    }
}

