using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages
{
	[Authorize(Roles = "Admin")]
	public class IndexModel : PageModel
	{
		private readonly SchedulerContext _context;

		public IndexModel(SchedulerContext context)
		{
			_context = context;
		}

		public IList<Service> UpcomingServices { get; set; } = default!;
		public int TotalMembers { get; set; }
		public int TotalCameras { get; set; }

		public async Task OnGetAsync()
		{
			// Get the 3 closest upcoming services
			var today = DateOnly.FromDateTime(DateTime.Today);
			UpcomingServices = await _context.Services
				.Where(s => s.Date >= today)
				.Include(s => s.Sessions)
				.OrderBy(s => s.Date)
				.ThenBy(s => s.StartTime)
				.Take(3)
				.ToListAsync();

			TotalMembers = await _context.Users.CountAsync(u => u.IsActive);
			TotalCameras = await _context.Cameras.CountAsync(c => c.IsActive);

		}
	}
}
