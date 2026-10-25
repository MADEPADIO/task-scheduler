using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages.Services
{
	[Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
		private readonly SchedulerContext _context;

		public IndexModel(SchedulerContext context)
		{
			_context = context;
		}

		public IList<Service> Services { get; set; } = default!;

		[BindProperty(SupportsGet = true)]
		public bool ShowPast { get; set; } = false;

		public async Task OnGetAsync()
		{
			var query = _context.Services.Include(s => s.Sessions).AsQueryable();

			// Filter to only today and future
			if (!ShowPast)
			{
				var today = DateOnly.FromDateTime(DateTime.Today);
				query = query.Where(s => s.Date >= today);
			}

			// Load services, order by date ascending upcoming first
			Services = await query
				.OrderBy(s => s.Date)
				.ThenBy(s => s.StartTime)
				.ToListAsync();
		}

		public async Task<IActionResult> OnPostDeleteAsync(Guid Id)
		{
			var service = await _context.Services.FindAsync(Id);
			if (service is not null)
			{
				// Because of Cascade Delete in the DbContext, this will also delete all Sessions and Assignments inside it.
				_context.Services.Remove(service);
				await _context.SaveChangesAsync();
			}

			return RedirectToPage("./Index");

		}
	}
}
