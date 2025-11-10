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
	public class SubscriptionPlansModel : PageModel
	{
		private readonly EVDbContext _db;
		public SubscriptionPlansModel(EVDbContext db) { _db = db; }

		public List<SubscriptionPlan> Plans { get; set; } = new();
		public List<(string Text, string Value)> BillingTypeOptions { get; set; } = new();
		public string? Query { get; set; }
		public string? BillingTypeFilter { get; set; }

		public async Task OnGetAsync(string? q = null, string? billingType = null)
		{
			Query = q;
			BillingTypeFilter = billingType;
			BillingTypeOptions = Enum.GetNames(typeof(BillingType)).Select(t => (t, t)).ToList();

			var query = _db.SubscriptionPlans.AsQueryable();

			if (!string.IsNullOrWhiteSpace(Query))
			{
				var k = Query.Trim().ToLower();
				query = query.Where(p => p.Name.ToLower().Contains(k) || (p.Description != null && p.Description.ToLower().Contains(k)));
			}

			if (!string.IsNullOrWhiteSpace(BillingTypeFilter) && Enum.TryParse<BillingType>(BillingTypeFilter, out var bt))
			{
				query = query.Where(p => p.BillingType == bt);
			}

			Plans = await query.OrderBy(p => p.Name).ToListAsync();
		}

		public async Task<IActionResult> OnPostCreateAsync()
		{
			var plan = new SubscriptionPlan
			{
				Id = Guid.NewGuid(),
				Name = Request.Form["Name"],
				Description = Request.Form["Description"],
				BillingType = Enum.TryParse<BillingType>(Request.Form["BillingType"], out var bt) ? bt : BillingType.Prepaid,
				PricePerMonth = decimal.TryParse(Request.Form["PricePerMonth"], out var ppm) ? ppm : null,
				PricePerKwh = decimal.TryParse(Request.Form["PricePerKwh"], out var ppk) ? ppk : null,
				IncludedKwh = decimal.TryParse(Request.Form["IncludedKwh"], out var ik) ? ik : null,
				IsActive = Request.Form["IsActive"].Count > 0,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};
			_db.SubscriptionPlans.Add(plan);
			await _db.SaveChangesAsync();
			return RedirectToPage();
		}

		public async Task<IActionResult> OnPostUpdateAsync()
		{
			if (!Guid.TryParse(Request.Form["Id"], out var id))
				return RedirectToPage();

			var plan = await _db.SubscriptionPlans.FindAsync(id);
			if (plan == null)
				return RedirectToPage();

			plan.Name = Request.Form["Name"];
			plan.Description = Request.Form["Description"];
			if (Enum.TryParse<BillingType>(Request.Form["BillingType"], out var bt))
				plan.BillingType = bt;
			plan.PricePerMonth = decimal.TryParse(Request.Form["PricePerMonth"], out var ppm) ? ppm : null;
			plan.PricePerKwh = decimal.TryParse(Request.Form["PricePerKwh"], out var ppk) ? ppk : null;
			plan.IncludedKwh = decimal.TryParse(Request.Form["IncludedKwh"], out var ik) ? ik : null;
			plan.IsActive = Request.Form["IsActive"].Count > 0;
			plan.UpdatedAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();
			return RedirectToPage();
		}

		public async Task<IActionResult> OnPostDeleteAsync(Guid id)
		{
			var plan = await _db.SubscriptionPlans.FindAsync(id);
			if (plan != null)
			{
				_db.SubscriptionPlans.Remove(plan);
				await _db.SaveChangesAsync();
			}
			return RedirectToPage();
		}
	}
}

