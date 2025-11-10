using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace PresentationLayer.Pages.Admin
{
	[Authorize(Roles = "Admin")]
	public class ReportsModel : PageModel
	{
		private readonly EVDbContext _db;
		public ReportsModel(EVDbContext db)
		{
			_db = db;
		}

		public int TotalStations { get; set; }
		public int TotalSpots { get; set; }
		public decimal TotalRevenue { get; set; }
		public int TotalSessions { get; set; }
		public Dictionary<string,int> StationByStatus { get; set; } = new();
		public Dictionary<string,int> SpotByStatus { get; set; } = new();
		public Dictionary<string,int> MaintenancesByStatus { get; set; } = new();
		public Dictionary<string,int> ErrorsByStatus { get; set; } = new();

		public class RevenueByStationItem
		{
			public string StationName { get; set; } = "";
			public int SessionCount { get; set; }
			public decimal TotalKwh { get; set; }
			public decimal Revenue { get; set; }
		}

		public class PeakHourItem
		{
			public int Hour { get; set; }
			public int SessionCount { get; set; }
			public decimal TotalKwh { get; set; }
			public decimal Revenue { get; set; }
		}

		public List<RevenueByStationItem> RevenueByStation { get; set; } = new();
		public List<PeakHourItem> PeakHours { get; set; } = new();
		public List<(Guid Id, string Name)> AllStations { get; set; } = new();
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }
		public Guid? StationId { get; set; }

		public async Task OnGet(DateTime? fromDate = null, DateTime? toDate = null, Guid? stationId = null)
		{
			// Convert dates to UTC if provided
			if (fromDate.HasValue)
			{
				if (fromDate.Value.Kind == DateTimeKind.Unspecified)
					FromDate = DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc);
				else if (fromDate.Value.Kind == DateTimeKind.Local)
					FromDate = fromDate.Value.ToUniversalTime();
				else
					FromDate = fromDate.Value;
			}
			else
			{
				FromDate = null;
			}

			if (toDate.HasValue)
			{
				if (toDate.Value.Kind == DateTimeKind.Unspecified)
					ToDate = DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc);
				else if (toDate.Value.Kind == DateTimeKind.Local)
					ToDate = toDate.Value.ToUniversalTime();
				else
					ToDate = toDate.Value;
			}
			else
			{
				ToDate = null;
			}

			StationId = stationId;

			TotalStations = await _db.ChargingStations.CountAsync();
			TotalSpots = await _db.ChargingSpots.CountAsync();

			var stations = await _db.ChargingStations
				.Select(s => new { s.Id, s.Name })
				.ToListAsync();
			AllStations = stations.Select(s => (s.Id, s.Name)).ToList();

			var sessionQuery = _db.ChargingSessions
				.Include(s => s.ChargingStation)
				.Where(s => s.Status == SessionStatus.Completed && s.TotalAmount.HasValue);

			if (FromDate.HasValue)
			{
				var fromDateUtc = FromDate.Value;
				if (fromDateUtc.Kind != DateTimeKind.Utc)
					fromDateUtc = DateTime.SpecifyKind(fromDateUtc, DateTimeKind.Utc);
				sessionQuery = sessionQuery.Where(s => s.StartTime >= fromDateUtc);
			}

			if (ToDate.HasValue)
			{
				var toDateUtc = ToDate.Value.AddDays(1);
				if (toDateUtc.Kind != DateTimeKind.Utc)
					toDateUtc = DateTime.SpecifyKind(toDateUtc, DateTimeKind.Utc);
				sessionQuery = sessionQuery.Where(s => s.StartTime <= toDateUtc);
			}

			if (StationId.HasValue)
				sessionQuery = sessionQuery.Where(s => s.ChargingStationId == StationId.Value);

			var sessions = await sessionQuery.ToListAsync();

			TotalSessions = sessions.Count;
			TotalRevenue = sessions.Sum(s => s.TotalAmount ?? 0);

			RevenueByStation = sessions
				.GroupBy(s => new { s.ChargingStationId, StationName = s.ChargingStation?.Name ?? "N/A" })
				.Select(g => new RevenueByStationItem
				{
					StationName = g.Key.StationName,
					SessionCount = g.Count(),
					TotalKwh = g.Sum(s => s.EnergyKwh ?? 0),
					Revenue = g.Sum(s => s.TotalAmount ?? 0)
				})
				.OrderByDescending(r => r.Revenue)
				.ToList();

			PeakHours = sessions
				.GroupBy(s => s.StartTime.Hour)
				.Select(g => new PeakHourItem
				{
					Hour = g.Key,
					SessionCount = g.Count(),
					TotalKwh = g.Sum(s => s.EnergyKwh ?? 0),
					Revenue = g.Sum(s => s.TotalAmount ?? 0)
				})
				.OrderByDescending(p => p.SessionCount)
				.Take(10)
				.ToList();

			StationByStatus = await _db.ChargingStations
				.GroupBy(s => s.Status)
				.Select(g => new { Name = g.Key.ToString(), Count = g.Count() })
				.ToDictionaryAsync(x => x.Name, x => x.Count);

			SpotByStatus = await _db.ChargingSpots
				.GroupBy(s => s.Status)
				.Select(g => new { Name = g.Key.ToString(), Count = g.Count() })
				.ToDictionaryAsync(x => x.Name, x => x.Count);

			MaintenancesByStatus = await _db.StationMaintenances
				.GroupBy(m => m.Status)
				.Select(g => new { Name = g.Key.ToString(), Count = g.Count() })
				.ToDictionaryAsync(x => x.Name, x => x.Count);

			ErrorsByStatus = await _db.StationErrors
				.GroupBy(e => e.Status)
				.Select(g => new { Name = g.Key.ToString(), Count = g.Count() })
				.ToDictionaryAsync(x => x.Name, x => x.Count);
		}
	}
}
