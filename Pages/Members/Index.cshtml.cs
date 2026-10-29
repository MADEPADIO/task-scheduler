using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages.Members
{
	[Authorize(Roles = "Admin")]
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
				if (!user.IsAdmin)
				{
					user.PasswordHash = null;
				}
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

		// Generate Temporary Password for an Admin
		public async Task<IActionResult> OnPostResetPasswordAsync(Guid id)
		{
			var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

			if (id == currentUserId)
			{
				TempData["Error"] = "To change you own password, use the 'Change Password' menu in the top right.";
				return RedirectToPage("./Index");
			}
			var user = await _context.Users.FindAsync(id);

			if (user is not null && user.IsAdmin)
			{
				var tempPassword = Guid.NewGuid().ToString("N").Substring(0, 8) + "Aa1!";

				// Hash it and save it to the database
				user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

				await _context.SaveChangesAsync();

				TempData["TempPassword"] = $"Temporary password for {user.Name}: {tempPassword}";
			}

			return RedirectToPage("./Index");
		}
    }
}
