using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Driver
{
    [Authorize(Roles = "EVDriver,Admin")]
    public class SessionsModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}

