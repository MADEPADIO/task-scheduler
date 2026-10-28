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

			// Get upcoming services
			UpcomingServices = await _context.Services
				.Where(s => s.Date >= today)
				.Include(s => s.Sessions.OrderBy(x => x.Order))
				.OrderBy(s => s.Date)
				.ThenBy(s => s.StartTime)
				.Take(5) // Show the next services
				.ToListAsync();

			var serviceIds = UpcomingServices.Select(s => s.Id).ToList();

			// Get this specific user's assignments for those services
			var assignments = await _context.Assignments
				.Include(a => a.Camera)
				.Where(a => a.UserId == userId && serviceIds.Contains(a.Session.ServiceId))
				.ToListAsync();

			foreach (var assignment in assignments)
			{
				// Map to a dictionary so the UI can easily find it.
				// Key: SessionId, CameraId
				MyAssignments[(assignment.SessionId, assignment.CameraId)] = assignment;
			}
			return Page();
		}

		// Handler to mark attending/absent
		public async Task<IActionResult> OnPostUpdateStatusAsync(Guid assignmentId, AssignmentStatus status, string? absenceReason)
		{
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!Guid.TryParse(userIdString, out Guid userId))
			{
				return RedirectToPage("/Account/Login");
			}

			var assignment = await _context.Assignments.FirstOrDefaultAsync(a => a.Id == assignmentId && a.UserId == userId);

			if (assignment != null)
			{
				assignment.Status = status;
				assignment.AbsenceReason = status == AssignmentStatus.Absent ? absenceReason : null;
				await _context.SaveChangesAsync();
				TempData["Success"] = "Your status has been updated.";
			}

			return RedirectToPage("./Index");
		}
	}
}
