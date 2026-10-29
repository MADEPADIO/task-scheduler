using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskSchedulerApp.Data;

namespace TaskSchedulerApp.Pages
{
	[Authorize]
    public class NotificationsModel : PageModel
    {
		private readonly SchedulerContext _context;

		public NotificationsModel(SchedulerContext context)
		{
			_context = context;
		}

		// Handler to fetch unread notifications via AJAX
		public async Task<IActionResult> OnGetUnreadAsync()
		{
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!Guid.TryParse(userIdString, out Guid userId))
			{
				return new JsonResult(new { count = 0, items = new string[] { } });
			}

			var notifications = await _context.Notifications
				.Where(n => n.UserId == userId && !n.IsRead)
				.OrderByDescending(n => n.CreatedAt)
				.Take(10)
				.Select(n => new 
				{ 
					n.Id, 
					n.Message, 
					LinkUrl = n.LinkUrl ?? "#", 
					Time = n.CreatedAt.ToString("MMM dd, hh:mm tt") 
				})
				.ToListAsync();

			return new JsonResult(new { count = notifications.Count, items = notifications });

		}

		// Handler to mark all notifications as read when the user clicks the bell
		public async Task<IActionResult> OnPostMarkReadAsync()
		{
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (Guid.TryParse(userIdString, out Guid userId))
			{
				var unread = await _context.Notifications
					.Where(n => n.UserId == userId && !n.IsRead)
					.ToListAsync();

				foreach (var n in unread)
				{
					n.IsRead = true;
				}
				await _context.SaveChangesAsync();
			}
			return new JsonResult(new { success = true });
		}
	}
}
