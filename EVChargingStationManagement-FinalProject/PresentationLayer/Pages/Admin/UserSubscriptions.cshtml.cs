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
	public class UserSubscriptionsModel : PageModel
	{
		private readonly EVDbContext _db;
		public UserSubscriptionsModel(EVDbContext db) { _db = db; }

		public class SubscriptionItem
		{
			public Guid Id { get; set; }
			public string UserOrCustomerName { get; set; } = "";
			public string PlanName { get; set; } = "";
			public string BillingType { get; set; } = "";
			public DateTime StartDate { get; set; }
			public DateTime? EndDate { get; set; }
			public string Status { get; set; } = "";
		}

		public class UserOption
		{
			public Guid Id { get; set; }
			public string Name { get; set; } = "";
			public string Email { get; set; } = "";
		}

		public List<SubscriptionItem> Subscriptions { get; set; } = new();
		public List<UserOption> AllUsers { get; set; } = new();
		public List<UserOption> AllCustomers { get; set; } = new();
		public List<SubscriptionPlan> AvailablePlans { get; set; } = new();
		public List<(string Text, string Value)> StatusOptions { get; set; } = new();
		public string? Query { get; set; }
		public string? StatusFilter { get; set; }

		public async Task OnGetAsync(string? q = null, string? status = null)
		{
			Query = q;
			StatusFilter = status;
			StatusOptions = Enum.GetNames(typeof(SubscriptionStatus)).Select(t => (t, t)).ToList();

			var query = _db.UserSubscriptions
				.Include(s => s.User)
				.Include(s => s.SubscriptionPlan)
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(Query))
			{
				var k = Query.Trim().ToLower();
				query = query.Where(s => (s.User != null && (s.User.FullName.ToLower().Contains(k) || s.User.Email.ToLower().Contains(k))));
			}

			if (!string.IsNullOrWhiteSpace(StatusFilter) && Enum.TryParse<SubscriptionStatus>(StatusFilter, out var st))
			{
				query = query.Where(s => s.Status == st);
			}

			var subs = await query.OrderByDescending(s => s.StartDate).Take(200).ToListAsync();

			Subscriptions = subs.Select(s => new SubscriptionItem
			{
				Id = s.Id,
				UserOrCustomerName = s.User?.FullName ?? "N/A",
				PlanName = s.SubscriptionPlan?.Name ?? "N/A",
				BillingType = s.SubscriptionPlan?.BillingType.ToString() ?? "",
				StartDate = s.StartDate,
				EndDate = s.EndDate,
				Status = s.Status.ToString()
			}).ToList();

			AllUsers = await _db.Users
				.Where(u => u.Role == UserRole.EVDriver || u.Role == UserRole.CSStaff || u.Role == UserRole.Admin)
				.Select(u => new UserOption { Id = u.Id, Name = u.FullName, Email = u.Email })
				.ToListAsync();

			AllCustomers = await _db.Customers
				.Select(c => new UserOption { Id = c.Id, Name = c.Name, Email = c.ContactEmail ?? "" })
				.ToListAsync();

			AvailablePlans = await _db.SubscriptionPlans
				.Where(p => p.IsActive)
				.OrderBy(p => p.Name)
				.ToListAsync();
		}

		public async Task<IActionResult> OnPostAssignAsync()
		{
			var userIdStr = Request.Form["UserId"];
			var customerIdStr = Request.Form["CustomerId"];
			var planIdStr = Request.Form["SubscriptionPlanId"];

			if (!Guid.TryParse(planIdStr, out var planId))
				return RedirectToPage();

			Guid? userId = null;
			if (!string.IsNullOrWhiteSpace(userIdStr) && Guid.TryParse(userIdStr, out var uid))
				userId = uid;

			Guid? customerId = null;
			if (!string.IsNullOrWhiteSpace(customerIdStr) && Guid.TryParse(customerIdStr, out var cid))
				customerId = cid;

			if (!userId.HasValue && !customerId.HasValue)
				return RedirectToPage();

            var startDateStr = Request.Form["StartDate"];
            DateTime startDate;
            if (!string.IsNullOrWhiteSpace(startDateStr) && DateTime.TryParse(startDateStr, out var parsedStart))
            {
                // Convert to UTC - assume input is in local time, convert to UTC
                if (parsedStart.Kind == DateTimeKind.Unspecified)
                    startDate = DateTime.SpecifyKind(parsedStart, DateTimeKind.Utc);
                else if (parsedStart.Kind == DateTimeKind.Local)
                    startDate = parsedStart.ToUniversalTime();
                else
                    startDate = parsedStart;
            }
            else
            {
                startDate = DateTime.UtcNow;
            }

            DateTime? endDate = null;
            var endDateStr = Request.Form["EndDate"];
            if (!string.IsNullOrWhiteSpace(endDateStr) && DateTime.TryParse(endDateStr, out var parsedEnd))
            {
                // Convert to UTC
                if (parsedEnd.Kind == DateTimeKind.Unspecified)
                    endDate = DateTime.SpecifyKind(parsedEnd, DateTimeKind.Utc);
                else if (parsedEnd.Kind == DateTimeKind.Local)
                    endDate = parsedEnd.ToUniversalTime();
                else
                    endDate = parsedEnd;
            }

            var sub = new UserSubscription
			{
				Id = Guid.NewGuid(),
				UserId = userId ?? Guid.Empty,
				SubscriptionPlanId = planId,
				StartDate = startDate,
				EndDate = endDate,
				Status = SubscriptionStatus.Active,
				AutoRenew = Request.Form["AutoRenew"].Count > 0,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			_db.UserSubscriptions.Add(sub);
			await _db.SaveChangesAsync();
			return RedirectToPage();
		}

		public async Task<IActionResult> OnPostCancelAsync(Guid id)
		{
			var sub = await _db.UserSubscriptions.FindAsync(id);
			if (sub != null)
			{
				sub.Status = SubscriptionStatus.Cancelled;
				sub.UpdatedAt = DateTime.UtcNow;
				await _db.SaveChangesAsync();
			}
			return RedirectToPage();
		}
	}
}

