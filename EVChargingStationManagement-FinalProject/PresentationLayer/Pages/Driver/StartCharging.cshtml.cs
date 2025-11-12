using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Driver
{
    [Authorize(Roles = "EVDriver,Admin")]
    public class StartChargingModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public Guid? SpotId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? ReservationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? StationId { get; set; }

        public void OnGet()
        {
        }
    }
}

