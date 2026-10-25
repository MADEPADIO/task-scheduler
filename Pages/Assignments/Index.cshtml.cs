using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages.Assignments
{
	[Authorize(Roles = "Admin")]
	public class IndexModel : PageModel
	{
		private readonly SchedulerContext _context;

		public IndexModel(SchedulerContext context)
		{
			_context = context;
		}

		public Service Service { get; set; } = default!;
		public IList<Camera> ActiveCameras { get; set; } = default!;
		public IList<User> ActiveMembers { get; set; } = default!;

		// This will hold the existing assignments for the view to pre-select dropdowns
		// Key: SessionId, Value: Dictionary of CameraId -> UserId
		public Dictionary<Guid, Dictionary<int, Guid?>> ExistingAssignments { get; set; } = new();

		public async Task<IActionResult> OnGetAsync(Guid id)
		{
			var service = await _context.Services
				.Include(s => s.Sessions.OrderBy(x => x.Order))
				.FirstOrDefaultAsync(m => m.Id == id);

			if (service == null) return NotFound();

			Service = service;

			ActiveCameras = await _context.Cameras.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
			ActiveMembers = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.Name).ToListAsync();

			// Fetch existing assignments for this service's sessions
			var existing = await _context.Assignments
				.Where(a => a.Session.ServiceId == id)
				.ToListAsync();

			// Build the dictionary for the view
			foreach (var session in Service.Sessions)
			{
				ExistingAssignments[session.Id] = new Dictionary<int, Guid?>();
				foreach (var cam in ActiveCameras)
				{
					var assignment = existing.FirstOrDefault(a => a.SessionId == session.Id && a.CameraId == cam.Id);
					ExistingAssignments[session.Id][cam.Id] = assignment?.UserId;
				}
			}

			return Page();
		}

		public async Task<IActionResult> OnPostSaveAsync(Guid serviceId)
		{
			var service = await _context.Services
				.Include(s => s.Sessions)
				.FirstOrDefaultAsync(m => m.Id == serviceId);

			if (service == null) return NotFound();

			// The form will submit keys in the format: "assign_{sessionId}_{cameraId}" with a value of the UserId
			var formKeys = Request.Form.Keys.Where(k => k.StartsWith("assign_"));

			// Get all current assignments for these sessions
			var sessionIds = service.Sessions.Select(s => s.Id).ToList();
			var existingAssignments = await _context.Assignments
				.Where(a => sessionIds.Contains(a.SessionId))
				.ToListAsync();

			foreach (var key in formKeys)
			{
				var parts = key.Split('_');
				if (parts.Length == 3 && Guid.TryParse(parts[1], out Guid sessionId) && int.TryParse(parts[2], out int cameraId))
				{
					var value = Request.Form[key].ToString();

					// If the dropdown was set to "Unassigned" (empty), remove the assignment
					if (string.IsNullOrEmpty(value) || !Guid.TryParse(value, out Guid userId))
					{
						var toRemove = existingAssignments.FirstOrDefault(a => a.SessionId == sessionId && a.CameraId == cameraId);
						if (toRemove != null) _context.Assignments.Remove(toRemove);
					}
					else
					{
						// If it exists, update it. If not, create it.
						var existing = existingAssignments.FirstOrDefault(a => a.SessionId == sessionId && a.CameraId == cameraId);
						if (existing != null)
						{
							existing.UserId = userId;
							// Reset status to pending if the user changed
							if (existing.UserId != userId)
							{
								existing.Status = AssignmentStatus.Pending;
								existing.AbsenceReason = null;
							}
						}
						else
						{
							_context.Assignments.Add(new Assignment
							{
								Id = Guid.NewGuid(),
								SessionId = sessionId,
								CameraId = cameraId,
								UserId = userId,
								Status = AssignmentStatus.Pending
							});
						}
					}
				}
			}

			await _context.SaveChangesAsync();

			TempData["Success"] = "Assignments saved successfully.";
			return RedirectToPage("./Index", new { id = serviceId });
		}
	}
}