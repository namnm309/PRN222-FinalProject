using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Staff
{
    [Authorize(Roles = "CSStaff,Admin")]
    public class ErrorsModel : PageModel
    {
        private readonly ILogger<ErrorsModel> _logger;

        public ErrorsModel(ILogger<ErrorsModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}

