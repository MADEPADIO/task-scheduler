using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages.AuditLogs
{
	[Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
	{
		private readonly SchedulerContext _context;

		public IndexModel(SchedulerContext context)
		{
			_context = context;
		}

		public IList<AuditLog> Logs { get; set; } = default!;

		public async Task OnGetAsync()
		{
			Logs = await _context.AuditLogs
				.OrderByDescending(l => l.Timestamp)
				.Take(100)
				.ToListAsync();
		}
	}
}
