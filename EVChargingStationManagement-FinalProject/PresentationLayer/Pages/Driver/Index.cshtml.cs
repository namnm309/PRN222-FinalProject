using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Driver
{
    [Authorize(Roles = "EVDriver,Admin")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}

