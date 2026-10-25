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

		public async Task OnGetAsync()
		{
			// Load services, order by date ascending upcoming first
			Services = await _context.Services
				.Include(s => s.Sessions)
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
