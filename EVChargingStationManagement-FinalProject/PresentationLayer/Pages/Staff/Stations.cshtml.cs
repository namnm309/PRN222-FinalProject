using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Staff
{
    [Authorize(Roles = "CSStaff,Admin")]
    public class StationsModel : PageModel
    {
        private readonly ILogger<StationsModel> _logger;

        public StationsModel(ILogger<StationsModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}

