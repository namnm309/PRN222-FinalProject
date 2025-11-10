using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Staff
{
    [Authorize(Roles = "CSStaff,Admin")]
    public class SessionsModel : PageModel
    {
        private readonly ILogger<SessionsModel> _logger;

        public SessionsModel(ILogger<SessionsModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}

