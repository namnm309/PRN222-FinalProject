using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
	public class StaffAssignmentsModel : PageModel
	{
		private readonly EVDbContext _db;
		public StaffAssignmentsModel(EVDbContext db) { _db = db; }

		public class AssignmentItem
		{
			public Guid Id { get; set; }
			public string StationName { get; set; } = "";
			public string StaffName { get; set; } = "";
			public string Role { get; set; } = "";
			public DateTime CreatedAt { get; set; }
		}

		public class StationOption
		{
			public Guid Id { get; set; }
			public string Name { get; set; } = "";
		}

		public class StaffOption
		{
			public Guid Id { get; set; }
			public string Name { get; set; } = "";
			public string Email { get; set; } = "";
		}

		public List<AssignmentItem> Assignments { get; set; } = new();
		public List<StationOption> AllStations { get; set; } = new();
		public List<StaffOption> AllStaff { get; set; } = new();
		public List<(string Text, string Value)> RoleOptions { get; set; } = new();
		public string? Query { get; set; }
		public string? RoleFilter { get; set; }

		public async Task OnGetAsync(string? q = null, string? role = null)
		{
			Query = q;
			RoleFilter = role;
			RoleOptions = Enum.GetNames(typeof(StationStaffRole)).Select(t => (t, t)).ToList();

			var query = _db.StationStaffAssignments
				.Include(a => a.ChargingStation)
				.Include(a => a.User)
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(Query))
			{
				var k = Query.Trim().ToLower();
				query = query.Where(a => (a.ChargingStation != null && a.ChargingStation.Name.ToLower().Contains(k))
					|| (a.User != null && a.User.FullName.ToLower().Contains(k)));
			}

			if (!string.IsNullOrWhiteSpace(RoleFilter) && Enum.TryParse<StationStaffRole>(RoleFilter, out var rf))
			{
				query = query.Where(a => a.Role == rf);
			}

			var ass = await query.OrderByDescending(a => a.CreatedAt).Take(200).ToListAsync();

			Assignments = ass.Select(a => new AssignmentItem
			{
				Id = a.Id,
				StationName = a.ChargingStation?.Name ?? "N/A",
				StaffName = a.User?.FullName ?? "N/A",
				Role = a.Role.ToString(),
				CreatedAt = a.CreatedAt
			}).ToList();

			AllStations = await _db.ChargingStations
				.Select(s => new StationOption { Id = s.Id, Name = s.Name })
				.OrderBy(s => s.Name)
				.ToListAsync();

			AllStaff = await _db.Users
				.Where(u => u.Role == UserRole.CSStaff)
				.Select(u => new StaffOption { Id = u.Id, Name = u.FullName, Email = u.Email })
				.OrderBy(u => u.Name)
				.ToListAsync();
		}

		public async Task<IActionResult> OnPostAssignAsync()
		{
			var stationIdStr = Request.Form["ChargingStationId"];
			var userIdStr = Request.Form["UserId"];
			var roleStr = Request.Form["Role"];

			if (!Guid.TryParse(stationIdStr, out var stationId) || !Guid.TryParse(userIdStr, out var userId))
				return RedirectToPage();

			if (!Enum.TryParse<StationStaffRole>(roleStr, out var role))
				return RedirectToPage();

			// Check if already assigned
			var existing = await _db.StationStaffAssignments
				.FirstOrDefaultAsync(a => a.ChargingStationId == stationId && a.UserId == userId);

			if (existing != null)
			{
				existing.Role = role;
				existing.UpdatedAt = DateTime.UtcNow;
			}
			else
			{
				var assignment = new StationStaffAssignment
				{
					Id = Guid.NewGuid(),
					ChargingStationId = stationId,
					UserId = userId,
					Role = role,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				};
				_db.StationStaffAssignments.Add(assignment);
			}

			await _db.SaveChangesAsync();
			return RedirectToPage();
		}

		public async Task<IActionResult> OnPostRemoveAsync(Guid id)
		{
			var assignment = await _db.StationStaffAssignments.FindAsync(id);
			if (assignment != null)
			{
				_db.StationStaffAssignments.Remove(assignment);
				await _db.SaveChangesAsync();
			}
			return RedirectToPage();
		}
	}
}

