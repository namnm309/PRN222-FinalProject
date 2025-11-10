using System;
using System.Linq;
using System.Threading.Tasks;
using BusinessLayer.Services;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace PresentationLayer.Pages.Admin
{
	[Authorize(Roles = "Admin")]
	public class StationsModel : PageModel
	{
		private readonly EVDbContext _db;

		public StationsModel(EVDbContext db)
		{
			_db = db;
		}

	[BindProperty(SupportsGet = true)]
	public string? Query { get; set; }
	[BindProperty(SupportsGet = true)]
	public StationStatus? Status { get; set; }
	[BindProperty(SupportsGet = true)]
	public SpotStatus? SpotStatusFilter { get; set; }
	[BindProperty(SupportsGet = true)]
	public int PageIndex { get; set; } = 1;
	[BindProperty(SupportsGet = true)]
	public int PageSize { get; set; } = 10;

	public List<ChargingStation> Stations { get; set; } = new();
	public int TotalCount { get; set; }
	public int TotalPages { get; set; }
	public bool HasPreviousPage => PageIndex > 1;
	public bool HasNextPage => PageIndex < TotalPages;

	public async Task OnGet()
	{
		var baseQuery = _db.ChargingStations.AsQueryable();

		if (!string.IsNullOrWhiteSpace(Query))
		{
			var key = Query.Trim().ToLower();
			baseQuery = baseQuery.Where(s => s.Name.ToLower().Contains(key) || s.Address.ToLower().Contains(key));
		}

		if (Status.HasValue)
			baseQuery = baseQuery.Where(s => s.Status == Status.Value);

		// Nếu filter SpotStatus: chỉ chứa các trạm có ÍT NHẤT 1 spot status phù hợp
		List<Guid>? stationIdsWithMatchingSpots = null;
		if (SpotStatusFilter.HasValue)
		{
			stationIdsWithMatchingSpots = await _db.ChargingSpots
					.Where(sp => sp.Status == SpotStatusFilter.Value)
					.Select(sp => sp.ChargingStationId)
					.Distinct().ToListAsync();
			baseQuery = baseQuery.Where(s => stationIdsWithMatchingSpots.Contains(s.Id));
		}

		// Tổng số lượng
		TotalCount = await baseQuery.CountAsync();
		TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
		if (PageIndex < 1) PageIndex = 1;
		if (PageIndex > TotalPages && TotalPages > 0) PageIndex = TotalPages;

		// Lấy danh sách Id đúng thứ tự
		var stationIdsThisPage = await baseQuery
			.OrderBy(s => s.Name)
			.Skip((PageIndex - 1) * PageSize)
			.Take(PageSize)
			.Select(s => s.Id)
			.ToListAsync();

		// Lấy trạm + spots với Id vừa phân trang
		var dictOrder = stationIdsThisPage.Select((id, idx) => new { id, idx }).ToDictionary(x => x.id, x => x.idx);
		var loaded = await _db.ChargingStations
			.Where(s => stationIdsThisPage.Contains(s.Id))
			.Include(s => s.ChargingSpots)
			.ToListAsync();
		// Sắp xếp lại đúng order (OrderBy ordinal position)
		Stations = loaded.OrderBy(s => dictOrder[s.Id]).ToList();

		// Nếu filter spot status: chỉ giữ lại spots phù hợp trong mỗi station
		if (SpotStatusFilter.HasValue)
		{
			foreach (var st in Stations)
				st.ChargingSpots = st.ChargingSpots.Where(x => x.Status == SpotStatusFilter.Value).ToList();
		}
	}

	public static string GetStationStatusText(StationStatus status)
	{
		return status switch
		{
			StationStatus.Active => "Hoạt động",
			StationStatus.Inactive => "Không hoạt động",
			StationStatus.Maintenance => "Bảo trì",
			StationStatus.Closed => "Đóng cửa",
			_ => status.ToString()
		};
	}

	public static string GetSpotStatusText(SpotStatus status)
	{
		return status switch
		{
			SpotStatus.Available => "Sẵn sàng",
			SpotStatus.Occupied => "Đang sử dụng",
			SpotStatus.Maintenance => "Bảo trì",
			SpotStatus.OutOfOrder => "Hỏng",
			SpotStatus.Reserved => "Đã đặt",
			_ => status.ToString()
		};
	}

		public async Task<IActionResult> OnPostToggleSpotAsync(Guid spotId)
		{
			var spot = await _db.ChargingSpots.FindAsync(spotId);
			if (spot == null)
				return RedirectToPage();

			// Toggle between OutOfOrder and Available (do not change if currently Occupied)
			if (spot.Status == SpotStatus.OutOfOrder)
			{
				spot.Status = SpotStatus.Available;
			}
			else if (spot.Status == SpotStatus.Available || spot.Status == SpotStatus.Maintenance || spot.Status == SpotStatus.Reserved)
			{
				spot.Status = SpotStatus.OutOfOrder;
			}
			// if Occupied, keep as is

			spot.UpdatedAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();
			return RedirectToPage(new { q = Query, status = Status, spotStatusFilter = SpotStatusFilter, pageIndex = PageIndex });
		}
	}
}
