using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Staff
{
    [Authorize(Roles = "CSStaff,Admin")]
    public class MaintenanceModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}

