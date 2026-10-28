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

		// CHANGED: Removed the '= false' default. 
		// A bool? can be true, false, or null. 
		// If it is null, it means the page just loaded and the user hasn't touched the switch yet.
		[BindProperty(SupportsGet = true)]
		public bool? OverrideRules { get; set; }

		public Dictionary<(Guid, int), HashSet<Guid>> DisabledUsers { get; set; } = new();

		// CHANGED: Now holds the full Assignment object so we can read the Status (Pending, Attending, Absent)
		public Dictionary<(Guid, int), Assignment?> ExistingAssignments { get; set; } = new();

		public async Task<IActionResult> OnGetAsync(Guid id)
		{
			await LoadDataAsync(id);
			if (Service == null) return NotFound();
			return Page();
		}

		private async Task LoadDataAsync(Guid id)
		{
			var service = await _context.Services
				.Include(s => s.Sessions.OrderBy(x => x.Order))
				.FirstOrDefaultAsync(m => m.Id == id);

			if (service == null) return;

			Service = service;
			ActiveCameras = await _context.Cameras.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
			ActiveMembers = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.Name).ToListAsync();

			// 1. Get all assignments for THIS service
			var currentServiceAssignments = await _context.Assignments
				.Where(a => a.Session.ServiceId == id)
				.ToListAsync();

			ExistingAssignments.Clear();
			foreach (var session in Service.Sessions)
			{
				foreach (var cam in ActiveCameras)
				{
					// Find the assignment for this specific session and camera
					var assignment = currentServiceAssignments.FirstOrDefault(a => a.SessionId == session.Id && a.CameraId == cam.Id);
					// Store the whole assignment object (or null if unassigned)
					ExistingAssignments[(session.Id, cam.Id)] = assignment;
				}
			}

			// 2. RULES ENGINE: Get assignments for the SAME DAY but OTHER SERVICES
			var otherServicesToday = await _context.Assignments
				.Include(a => a.Session)
				.Where(a => a.Session.Service.Date == Service.Date && a.Session.ServiceId != Service.Id)
				.ToListAsync();

			var userServiceCount = otherServicesToday
				.GroupBy(a => a.UserId)
				.ToDictionary(g => g.Key, g => g.Select(a => a.Session.ServiceId).Distinct().Count());

			var userCameras = otherServicesToday
				.GroupBy(a => a.UserId)
				.ToDictionary(g => g.Key, g => g.Select(a => a.CameraId).ToHashSet());

			// NEW: Auto-detect if the current assignments in the database ALREADY violate the rules
			bool hasExistingViolations = false;
			foreach (var assignment in currentServiceAssignments)
			{
				// Check Max 2 Services Rule
				if (userServiceCount.TryGetValue(assignment.UserId, out int count) && count >= 2)
				{
					hasExistingViolations = true;
					break;
				}
				// Check Same Camera Rule
				if (userCameras.TryGetValue(assignment.UserId, out var cams) && cams.Contains(assignment.CameraId))
				{
					hasExistingViolations = true;
					break;
				}
			}

			// If the page just loaded (OverrideRules is null) AND we found violations in the database,
			// automatically turn the Override switch ON so the Admin doesn't get yelled at for their own override.
			if (!OverrideRules.HasValue && hasExistingViolations)
			{
				OverrideRules = true;
			}

			var usersInCurrentService = currentServiceAssignments.Select(a => a.UserId).ToHashSet();

			DisabledUsers.Clear();
			foreach (var session in Service.Sessions)
			{
				foreach (var cam in ActiveCameras)
				{
					var disabledList = new HashSet<Guid>();

					foreach (var member in ActiveMembers)
					{
						bool isUserInCurrentService = usersInCurrentService.Contains(member.Id);

						// RULE 1: Max 2 Services
						// CHANGED: We check if OverrideRules is NOT true. If it's null (default) or false, we enforce the rule.
						if (!isUserInCurrentService && OverrideRules != true)
						{
							if (userServiceCount.TryGetValue(member.Id, out int count) && count >= 2)
							{
								disabledList.Add(member.Id);
								continue;
							}
						}

						// RULE 2: Same camera
						// CHANGED: Same here. Only enforce if OverrideRules is NOT true.
						if (OverrideRules != true && userCameras.TryGetValue(member.Id, out var cams) && cams.Contains(cam.Id))
						{
							disabledList.Add(member.Id);
						}
					}
					DisabledUsers[(session.Id, cam.Id)] = disabledList;
				}
			}
		}

		public async Task<IActionResult> OnPostSaveAsync(Guid serviceId)
		{
			var service = await _context.Services
				.Include(s => s.Sessions)
				.FirstOrDefaultAsync(m => m.Id == serviceId);

			if (service == null) return NotFound();

			var formKeys = Request.Form.Keys.Where(k => k.StartsWith("assign_")).ToList();
			var submittedAssignments = new List<(Guid SessionId, int CameraId, Guid? UserId)>();

			foreach (var key in formKeys)
			{
				var parts = key.Split('_');
				if (parts.Length == 3 && Guid.TryParse(parts[1], out Guid sessionId) && int.TryParse(parts[2], out int cameraId))
				{
					var value = Request.Form[key].ToString();
					Guid? userId = (string.IsNullOrEmpty(value) || !Guid.TryParse(value, out Guid parsedId)) ? null : parsedId;
					submittedAssignments.Add((sessionId, cameraId, userId));
				}
			}

			var duplicate = submittedAssignments
				.Where(a => a.UserId.HasValue)
				.GroupBy(a => new { a.SessionId, a.UserId })
				.Where(g => g.Count() > 1)
				.FirstOrDefault();

			if (duplicate != null)
			{
				var member = await _context.Users.FindAsync(duplicate.Key.UserId);
				var session = await _context.Sessions.FindAsync(duplicate.Key.SessionId);
				TempData["Error"] = $"Cannot assign {member?.Name} to multiple cameras in the same session ({session?.Name}).";
				await LoadDataAsync(serviceId);
				return Page();
			}

			var sessionIds = service.Sessions.Select(s => s.Id).ToList();
			var existingAssignments = await _context.Assignments
				.Where(a => sessionIds.Contains(a.SessionId))
				.ToListAsync();

			var toRemove = new List<Assignment>();
			foreach (var sub in submittedAssignments)
			{
				var existing = existingAssignments.FirstOrDefault(a => a.SessionId == sub.SessionId && a.CameraId == sub.CameraId);
				if (existing != null && existing.UserId != sub.UserId)
				{
					toRemove.Add(existing);
				}
			}

			if (toRemove.Any())
			{
				_context.Assignments.RemoveRange(toRemove);
				await _context.SaveChangesAsync();
			}

			foreach (var sub in submittedAssignments)
			{
				if (!sub.UserId.HasValue) continue;

				var stillExists = await _context.Assignments.AnyAsync(a => a.SessionId == sub.SessionId && a.CameraId == sub.CameraId);

				if (!stillExists)
				{
					_context.Assignments.Add(new Assignment
					{
						Id = Guid.NewGuid(),
						SessionId = sub.SessionId,
						CameraId = sub.CameraId,
						UserId = sub.UserId.Value,
						Status = AssignmentStatus.Pending
					});

					var session = service.Sessions.First(s => s.Id == sub.SessionId);
					var camera = await _context.Cameras.FindAsync(sub.CameraId);

					_context.Notifications.Add(new Notification
					{
						Id = Guid.NewGuid(),
						UserId = sub.UserId.Value,
						Message = $"You have been assigned to {camera?.Name} for {session.Name} on {service.Date.ToString("MMM dd")}.",
						CreatedAt = DateTime.UtcNow
					});
				}
			}

			await _context.SaveChangesAsync();

			TempData["Success"] = "Assignments saved successfully.";
			return RedirectToPage("./Index", new { id = serviceId });
		}
	}
}