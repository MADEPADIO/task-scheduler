using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages.Member
{
	[Authorize]
	public class IndexModel : PageModel
	{
		private readonly SchedulerContext _context;

		public IndexModel(SchedulerContext context)
		{
			_context = context;
		}

		public IList<Service> UpcomingServices { get; set; } = default!;
		public Dictionary<(Guid, int), Assignment> MyAssignments { get; set; } = new();

		public async Task<IActionResult> OnGetAsync()
		{
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!Guid.TryParse(userIdString, out Guid userId))
			{
				return RedirectToPage("/Account/Login");
			}

			var today = DateOnly.FromDateTime(DateTime.Today);

			UpcomingServices = await _context.Services
				.Where(s => s.Date >= today)
				.Include(s => s.Sessions.OrderBy(x => x.Order))
				.OrderBy(s => s.Date)
				.ThenBy(s => s.StartTime)
				.Take(3)
				.ToListAsync();

			var serviceIds = UpcomingServices.Select(s => s.Id).ToList();

			var assignments = await _context.Assignments
				.Include(a => a.Camera)
				.Where(a => a.UserId == userId && serviceIds.Contains(a.Session.ServiceId))
				.ToListAsync();

			foreach (var assignment in assignments)
			{
				MyAssignments[(assignment.SessionId, assignment.CameraId)] = assignment;
			}

			return Page();
		}

		// NEW: Handles saving all statuses at once
		public async Task<IActionResult> OnPostUpdateAllStatusesAsync()
		{
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!Guid.TryParse(userIdString, out Guid userId)) return RedirectToPage("/Account/Login");

			var user = await _context.Users.FindAsync(userId);
			var admins = await _context.Users.Where(u => u.IsAdmin && u.IsActive).ToListAsync();

			// Loop through all keys in the submitted form
			foreach (var key in Request.Form.Keys)
			{
				if (key.StartsWith("status_"))
				{
					var idString = key.Replace("status_", "");
					if (!Guid.TryParse(idString, out Guid assignmentId)) continue;

					var statusString = Request.Form[key].ToString();
					var reason = Request.Form[$"reason_{assignmentId}"].ToString();

					if (!Enum.TryParse<AssignmentStatus>(statusString, out var newStatus)) continue;

					// Find the assignment in the database
					var assignment = await _context.Assignments
						.Include(a => a.Session)
						.Include(a => a.Camera)
						.FirstOrDefaultAsync(a => a.Id == assignmentId && a.UserId == userId);

					if (assignment != null)
					{
						// Check if they are NEWLY marking themselves absent (to prevent duplicate notifications)
						bool isNewlyAbsent = newStatus == AssignmentStatus.Absent && assignment.Status != AssignmentStatus.Absent;

						assignment.Status = newStatus;
						assignment.AbsenceReason = newStatus == AssignmentStatus.Absent ? reason : null;

						// If newly absent, notify the Admins
						if (isNewlyAbsent)
						{
							foreach (var admin in admins)
							{
								var sessionName = assignment.Session?.Name ?? "Unknown Session";
								var cameraName = assignment.Camera?.Name ?? "Unknown Camera";

								_context.Notifications.Add(new Notification
								{
									Id = Guid.NewGuid(),
									UserId = admin.Id,
									Message = $"{user?.Name} marked absent for {sessionName} ({cameraName}). Reason: {reason}",
									LinkUrl = $"/Assignments/Index/{assignment.Session.ServiceId}",
									CreatedAt = DateTime.UtcNow
								});
							}
						}
					}
				}
			}

			await _context.SaveChangesAsync();
			TempData["Success"] = "Your schedule has been updated successfully.";
			return RedirectToPage("./Index");
		}
	}
}