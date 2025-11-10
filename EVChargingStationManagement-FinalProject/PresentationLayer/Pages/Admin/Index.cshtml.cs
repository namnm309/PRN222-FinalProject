using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace PresentationLayer.Pages.Admin
{
	[Authorize(Roles = "Admin")]
	public class IndexModel : PageModel
	{
		private readonly EVDbContext _db;
		public IndexModel(EVDbContext db) { _db = db; }

		// KPI Statistics
		public int TotalStations { get; set; }
		public int TotalSpots { get; set; }
		public int ActiveSpots { get; set; }
		public int TotalUsers { get; set; }
		public int TotalStaff { get; set; }
		public int TotalCustomers { get; set; }
		public int TotalSubscriptions { get; set; }
		public int ActiveSubscriptions { get; set; }
		public decimal TotalRevenue { get; set; }
		public int TotalSessions { get; set; }
		public int OpenMaintenances { get; set; }
		public int ResolvedErrors { get; set; }

		// Recent Data
		public List<ChargingStation> RecentStations { get; set; } = new();
		public List<ChargingSession> RecentSessions { get; set; } = new();
		public List<UserSubscription> RecentSubscriptions { get; set; } = new();
		public List<StationMaintenance> RecentMaintenances { get; set; } = new();

		// Status Breakdown
		public Dictionary<string, int> StationStatusBreakdown { get; set; } = new();
		public Dictionary<string, int> SpotStatusBreakdown { get; set; } = new();

		public async Task OnGetAsync()
		{
			// KPI Statistics
			TotalStations = await _db.ChargingStations.CountAsync();
			TotalSpots = await _db.ChargingSpots.CountAsync();
			ActiveSpots = await _db.ChargingSpots.CountAsync(s => s.Status == SpotStatus.Available);
			TotalUsers = await _db.Users.CountAsync();
			TotalStaff = await _db.Users.CountAsync(u => u.Role == UserRole.CSStaff);
			TotalCustomers = await _db.Customers.CountAsync();
			TotalSubscriptions = await _db.UserSubscriptions.CountAsync();
			ActiveSubscriptions = await _db.UserSubscriptions.CountAsync(s => s.Status == SubscriptionStatus.Active);
			
			var completedSessions = await _db.ChargingSessions
				.Where(s => s.Status == SessionStatus.Completed && s.TotalAmount.HasValue)
				.ToListAsync();
			TotalRevenue = completedSessions.Sum(s => s.TotalAmount ?? 0);
			TotalSessions = completedSessions.Count;

			OpenMaintenances = await _db.StationMaintenances
				.CountAsync(m => m.Status == MaintenanceStatus.Scheduled || m.Status == MaintenanceStatus.InProgress);
			ResolvedErrors = await _db.StationErrors
				.CountAsync(e => e.Status == ErrorStatus.Resolved);

			// Recent Stations (5 most recent)
			RecentStations = await _db.ChargingStations
				.OrderByDescending(s => s.CreatedAt)
				.Take(5)
				.ToListAsync();

			// Recent Sessions (10 most recent completed)
			RecentSessions = await _db.ChargingSessions
				.Include(s => s.User)
				.Include(s => s.ChargingStation)
				.Where(s => s.Status == SessionStatus.Completed)
				.OrderByDescending(s => s.EndTime ?? s.StartTime)
				.Take(10)
				.ToListAsync();

			// Recent Subscriptions (5 most recent active)
			RecentSubscriptions = await _db.UserSubscriptions
				.Include(s => s.User)
				.Include(s => s.SubscriptionPlan)
				.Where(s => s.Status == SubscriptionStatus.Active)
				.OrderByDescending(s => s.StartDate)
				.Take(5)
				.ToListAsync();

			// Recent Maintenances (5 most recent)
			RecentMaintenances = await _db.StationMaintenances
				.Include(m => m.ChargingStation)
				.OrderByDescending(m => m.CreatedAt)
				.Take(5)
				.ToListAsync();

			// Status Breakdown
			StationStatusBreakdown = await _db.ChargingStations
				.GroupBy(s => s.Status)
				.Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
				.ToDictionaryAsync(x => x.Status, x => x.Count);

			SpotStatusBreakdown = await _db.ChargingSpots
				.GroupBy(s => s.Status)
				.Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
				.ToDictionaryAsync(x => x.Status, x => x.Count);
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
	}
}

