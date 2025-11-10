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
	public class UsersModel : PageModel
	{
		private readonly EVDbContext _db;

		public UsersModel(EVDbContext db)
		{
			_db = db;
		}

		public class UserMetrics
		{
			public int OpenMaintenances { get; set; }
			public int ResolvedErrors { get; set; }
		}

		public class AccountItem
		{
			public string Kind { get; set; } = "User"; // User or Customer
			public Guid? UserId { get; set; }
			public Guid? CustomerId { get; set; }
			public string Name { get; set; } = "";
			public string? Username { get; set; }
			public string? Email { get; set; }
			public string Role { get; set; } = "";
			public string RoleCss { get; set; } = "bg-secondary";
			public bool? IsActive { get; set; }
		}

		[BindProperty(SupportsGet = true)]
		public string? Query { get; set; }

		[BindProperty(SupportsGet = true)]
		public string? RoleFilter { get; set; }

		[BindProperty(SupportsGet = true)]
		public string? StatusFilter { get; set; }

		public List<AccountItem> AllAccounts { get; set; } = new();
		public List<(string Text, string Value)> RoleOptions { get; set; } = new();
		public Dictionary<Guid, UserMetrics> MetricsByUser { get; set; } = new();

		// KPIs
		public int TotalStaff { get; set; }
		public int ActiveStaff { get; set; }
		public int OpenMaintenances { get; set; }
		public int ErrorsResolved { get; set; }

		public async Task OnGet()
		{
			// role options - include Customer
			RoleOptions = new List<(string, string)>
			{
				("Admin", "Admin"),
				("CSStaff", "CSStaff"),
				("Customer", "Customer")
			};

			var list = new List<AccountItem>();

			// Get Users (Admin and Staff)
			var userQuery = _db.Users.Where(u => u.Role == UserRole.CSStaff || u.Role == UserRole.Admin);

			if (!string.IsNullOrWhiteSpace(Query))
			{
				var q = Query.Trim().ToLower();
				userQuery = userQuery.Where(u => u.FullName.ToLower().Contains(q)
					|| u.Email.ToLower().Contains(q)
					|| u.Username.ToLower().Contains(q));
			}

			if (!string.IsNullOrWhiteSpace(RoleFilter))
			{
				if (RoleFilter == "Admin" && Enum.TryParse<UserRole>(RoleFilter, out var adminRole))
					userQuery = userQuery.Where(u => u.Role == adminRole);
				else if (RoleFilter == "CSStaff")
					userQuery = userQuery.Where(u => u.Role == UserRole.CSStaff);
				else if (RoleFilter == "Customer")
					userQuery = userQuery.Where(u => false); // filter out all users
			}

			if (!string.IsNullOrWhiteSpace(StatusFilter))
			{
				if (StatusFilter.Equals("active", StringComparison.OrdinalIgnoreCase))
					userQuery = userQuery.Where(u => u.IsActive);
				else if (StatusFilter.Equals("inactive", StringComparison.OrdinalIgnoreCase))
					userQuery = userQuery.Where(u => !u.IsActive);
			}

			var users = await userQuery
				.OrderByDescending(u => u.Role)
				.ThenBy(u => u.FullName)
				.Take(200)
				.ToListAsync();

			list.AddRange(users.Select(u => new AccountItem
			{
				Kind = "User",
				UserId = u.Id,
				Name = u.FullName,
				Username = u.Username,
				Email = u.Email,
				Role = u.Role == UserRole.Admin ? "Admin" : "Staff",
				RoleCss = u.Role == UserRole.Admin ? "bg-dark text-white" : "bg-info text-dark",
				IsActive = u.IsActive
			}));

			// Get Customers
			var custQuery = _db.Customers.AsQueryable();

			if (!string.IsNullOrWhiteSpace(Query))
			{
				var q = Query.Trim().ToLower();
				custQuery = custQuery.Where(c => c.Name.ToLower().Contains(q)
					|| (c.ContactEmail != null && c.ContactEmail.ToLower().Contains(q))
					|| (c.Phone != null && c.Phone.Contains(q)));
			}

			if (RoleFilter == "Customer")
				; // keep all customers
			else if (!string.IsNullOrWhiteSpace(RoleFilter))
				custQuery = custQuery.Where(c => false); // filter out all customers

			var customers = await custQuery
				.OrderBy(c => c.Name)
				.Take(200)
				.ToListAsync();

			list.AddRange(customers.Select(c => new AccountItem
			{
				Kind = "Customer",
				CustomerId = c.Id,
				Name = c.Name,
				Email = c.ContactEmail,
				Role = "Customer",
				RoleCss = "bg-success text-white",
				IsActive = null
			}));

			AllAccounts = list.OrderByDescending(a => a.Role).ThenBy(a => a.Name).ToList();

			var userIds = users.Select(u => u.Id).ToList();
			await ComputeMetricsAsync(userIds);
			await ComputeKpisAsync();
		}

		private async Task ComputeMetricsAsync(List<Guid> userIds)
		{
			MetricsByUser = await _db.Users
				.Where(u => userIds.Contains(u.Id))
				.Select(u => new
				{
					u.Id,
					OpenMaintenances = _db.StationMaintenances.Count(m => m.AssignedToUserId == u.Id && (m.Status == MaintenanceStatus.Scheduled || m.Status == MaintenanceStatus.InProgress)),
					ResolvedErrors = _db.StationErrors.Count(e => e.ResolvedByUserId == u.Id)
				})
				.ToDictionaryAsync(x => x.Id, x => new UserMetrics
				{
					OpenMaintenances = x.OpenMaintenances,
					ResolvedErrors = x.ResolvedErrors
				});
		}

		private async Task ComputeKpisAsync()
		{
			TotalStaff = await _db.Users.CountAsync(u => u.Role == UserRole.CSStaff);
			ActiveStaff = await _db.Users.CountAsync(u => u.Role == UserRole.CSStaff && u.IsActive);
			OpenMaintenances = await _db.StationMaintenances.CountAsync(m => m.Status == MaintenanceStatus.Scheduled || m.Status == MaintenanceStatus.InProgress);
			ErrorsResolved = await _db.StationErrors.CountAsync(e => e.ResolvedByUserId != null);
		}

		public async Task<IActionResult> OnPostToggleActiveAsync(Guid userId)
		{
			var user = await _db.Users.FindAsync(userId);
			if (user == null)
				return RedirectToPage();

			user.IsActive = !user.IsActive;
			user.UpdatedAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();
			return RedirectToPage(new { q = Query, role = RoleFilter, status = StatusFilter });
		}

		public async Task<IActionResult> OnPostPromoteAsync(Guid userId)
		{
			var user = await _db.Users.FindAsync(userId);
			if (user == null)
				return RedirectToPage();

			user.Role = user.Role == UserRole.EVDriver ? UserRole.CSStaff : UserRole.Admin;
			user.UpdatedAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();
			return RedirectToPage(new { q = Query, role = RoleFilter, status = StatusFilter });
		}

		public async Task<IActionResult> OnPostDemoteAsync(Guid userId)
		{
			var user = await _db.Users.FindAsync(userId);
			if (user == null)
				return RedirectToPage();

			user.Role = user.Role == UserRole.Admin ? UserRole.CSStaff : UserRole.EVDriver;
			user.UpdatedAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();
			return RedirectToPage(new { q = Query, role = RoleFilter, status = StatusFilter });
		}

		public async Task<IActionResult> OnPostDeleteCustomerAsync(Guid id)
		{
			var customer = await _db.Customers.FindAsync(id);
			if (customer != null)
			{
				_db.Customers.Remove(customer);
				await _db.SaveChangesAsync();
			}
			return RedirectToPage(new { q = Query, role = RoleFilter, status = StatusFilter });
		}
	}
}
