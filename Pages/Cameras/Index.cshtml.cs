using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages.Cameras
{
	[Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
		private readonly SchedulerContext _context;

		public IndexModel(SchedulerContext context)
		{
			_context = context;
		}

		public IList<Camera> Cameras { get; set; } = default!;

		public async Task OnGetAsync()
		{
			Cameras = await _context.Cameras.OrderBy(c => c.Name).ToListAsync();
		}
    }
}
