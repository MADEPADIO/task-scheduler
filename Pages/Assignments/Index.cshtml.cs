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

		// NEW: Binds the ?OverrideRules=true/false from the URL to this property.
		// When true, the Max 2 Services rule is ignored in the UI dropdowns.
		[BindProperty(SupportsGet = true)]
		public bool OverrideRules { get; set; } = false;

		// This will hold the existing assignments for the view to pre-select dropdowns
		// Key: SessionId, CameraId. Value: List of UserIds who CANNOT be assigned to this cell
		public Dictionary<(Guid, int), HashSet<Guid>> DisabledUsers { get; set; } = new();
		public Dictionary<Guid, Dictionary<int, Guid?>> ExistingAssignments { get; set; } = new();

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

			// 1. Get all assignments for THIS service to populate the dropdowns
			var currentServiceAssignments = await _context.Assignments
				.Where(a => a.Session.ServiceId == id)
				.ToListAsync();

			ExistingAssignments.Clear();
			foreach (var session in Service.Sessions)
			{
				ExistingAssignments[session.Id] = new Dictionary<int, Guid?>();
				foreach (var cam in ActiveCameras)
				{
					var assignment = currentServiceAssignments.FirstOrDefault(a => a.SessionId == session.Id && a.CameraId == cam.Id);
					ExistingAssignments[session.Id][cam.Id] = assignment?.UserId;
				}
			}

			// 2. RULES ENGINE: Get assignments for the SAME DAY but OTHER SERVICES
			var otherServicesToday = await _context.Assignments
				.Include(a => a.Session)
				.Where(a => a.Session.Service.Date == Service.Date && a.Session.ServiceId != Service.Id)
				.ToListAsync();

			// Count distinct services each user is working today
			var userServiceCount = otherServicesToday
				.GroupBy(a => a.UserId)
				.ToDictionary(g => g.Key, g => g.Select(a => a.Session.ServiceId).Distinct().Count());

			// Track which cameras each user has used today
			var userCameras = otherServicesToday
				.GroupBy(a => a.UserId)
				.ToDictionary(g => g.Key, g => g.Select(a => a.CameraId).ToHashSet());

			// Users already working in THIS service (so we don't block them from adding more sessions here)
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

						// RULE 1: Max 2 Services per day
						// We check !OverrideRules here. If the Admin flipped the switch, we skip this check.
						if (!isUserInCurrentService && !OverrideRules)
						{
							if (userServiceCount.TryGetValue(member.Id, out int count) && count >= 2)
							{
								disabledList.Add(member.Id);
								continue; // No need to check camera rule if they are already maxed out
							}
						}

						// RULE 2: Same camera across services on the same day
						// Notice we do NOT check OverrideRules here. This rule is a hard block regardless of override status.
						if (userCameras.TryGetValue(member.Id, out var cams) && cams.Contains(cam.Id))
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
			// The validation logic here catches rogue submissions and prevents unique constraint crashes.

			var service = await _context.Services
				.Include(s => s.Sessions)
				.FirstOrDefaultAsync(m => m.Id == serviceId);

			if (service == null) return NotFound();

			var formKeys = Request.Form.Keys.Where(k => k.StartsWith("assign_")).ToList();
			var submittedAssignments = new List<(Guid SessionId, int CameraId, Guid? UserId)>();

			// 1. Parse the form data
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

			// 2. Validate: Check if the same user is assigned to multiple cameras in the SAME session
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

			// 3. First Pass: Remove assignments that were cleared or changed (prevents unique constraint crash on swaps)
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
				await _context.SaveChangesAsync(); // Flush deletes first
			}

			// 4. Second Pass: Add new assignments
			foreach (var sub in submittedAssignments)
			{
				if (!sub.UserId.HasValue) continue; // Ignore unassigned

				// We already deleted the ones that changed, so if it exists now, it didn't change.
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
				}
			}

			await _context.SaveChangesAsync();

			TempData["Success"] = "Assignments saved successfully.";
			return RedirectToPage("./Index", new { id = serviceId });
		}
	}
}