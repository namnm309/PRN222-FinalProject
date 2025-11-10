using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace PresentationLayer.Pages.Admin
{
	[Authorize(Roles = "Admin")]
	public class SpotsMapModel : PageModel
	{
		private readonly EVDbContext _db;
		public SpotsMapModel(EVDbContext db) { _db = db; }

		public void OnGet() { }

		[IgnoreAntiforgeryToken]
		public async Task<IActionResult> OnGetDataAsync(string? status)
		{
			try
			{
				// Set content type to JSON explicitly
				Response.ContentType = "application/json";

				SpotStatus? statusFilter = null;
				if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SpotStatus>(status, out var parsed))
					statusFilter = parsed;

				var query = _db.ChargingSpots
					.Include(s => s.ChargingStation)
					.Where(s => s.ChargingStation != null 
						&& s.ChargingStation.Latitude.HasValue 
						&& s.ChargingStation.Longitude.HasValue
						&& s.ChargingStation.Latitude != 0 
						&& s.ChargingStation.Longitude != 0)
					.AsQueryable();

				if (statusFilter.HasValue)
					query = query.Where(s => s.Status == statusFilter.Value);

				var rows = await query
					.Select(s => new
					{
						id = s.Id,
						spotNumber = s.SpotNumber,
						status = s.Status.ToString(),
						stationName = s.ChargingStation!.Name,
						lat = s.ChargingStation!.Latitude!.Value,
						lng = s.ChargingStation!.Longitude!.Value
					})
					.ToListAsync();

				return new JsonResult(rows);
			}
			catch (Exception ex)
			{
				Response.ContentType = "application/json";
				return new JsonResult(new { error = ex.Message, stackTrace = ex.StackTrace }) { StatusCode = 500 };
			}
		}

		[ValidateAntiForgeryToken]
		public async Task<IActionResult> OnPostToggleAsync(Guid spotId)
		{
			var spot = await _db.ChargingSpots.FindAsync(spotId);
			if (spot == null) return new JsonResult(new { ok = false });

			if (spot.Status == SpotStatus.OutOfOrder)
				spot.Status = SpotStatus.Available;
			else if (spot.Status == SpotStatus.Available || spot.Status == SpotStatus.Maintenance || spot.Status == SpotStatus.Reserved)
				spot.Status = SpotStatus.OutOfOrder;

			spot.UpdatedAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();
			return new JsonResult(new { ok = true });
		}
	}
}
