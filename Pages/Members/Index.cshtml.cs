using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages.Members
{
	[Authorize]
    public class IndexModel : PageModel
    {
		private readonly SchedulerContext _context;

		public IndexModel(SchedulerContext context)
		{
			_context = context;
		}

		public IList<User> Members { get; set; } = default!;

		public async Task OnGetAsync()
		{
			// Load all users, order by Name
			Members = await _context.Users
				.Where(u => u.IsActive)
				.OrderBy(u => u.Name)
				.ToListAsync();
		}

		// Inline toggle for Admin status
		public async Task<IActionResult> OnPostToggleAdminAsync(Guid id)
		{
			var user = await _context.Users.FindAsync(id);
			if (user is not null)
			{
				var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

				// Prevent the current admin from demoting themselves (locks them out)
				if (user.Id == currentUserId && user.IsAdmin)
				{
					TempData["Error"] = "You cannot remove you own admin privileges.";
					return RedirectToPage("./Index");

				}

				user.IsAdmin = !user.IsAdmin;
				await _context.SaveChangesAsync();
			}

			return RedirectToPage("./Index");

		}

		// Inline Delete
		public async Task<IActionResult> OnPostDeleteAsync(Guid id)
		{
			var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
			
			if (id == currentUserId)
			{
				TempData["Error"] = "You cannot deactivate your own account.";
				return RedirectToPage("./Index");

			}

			var user = await _context.Users.FindAsync(id);
			if (user is not null)
			{
				user.IsActive = false;
				await _context.SaveChangesAsync();
			}
			return RedirectToPage("./Index");

		}
    }
}
