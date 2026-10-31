using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages.Services
{
	[Authorize(Roles = "Admin")]
    public class GenerateModel : PageModel
    {
		private readonly SchedulerContext _context;

		public GenerateModel(SchedulerContext context)
		{
			_context = context;
		}

		[BindProperty]
		public DateOnly StartDate { get; set; }

		[BindProperty]
		[Range(1, 12, ErrorMessage = "You can generate between 1 and 12 weeks at a time.")]
		public int NumberOfWeeks { get; set; } = 4;

		public IActionResult OnGet()
		{
			// Default to the upcoming Sunday
			int diff = (7 + (DayOfWeek.Sunday - DateTime.Today.DayOfWeek)) % 7;
			StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(diff == 0 ? 7 : diff));
			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			// Safety check: Ensure the selected date is actually a Sunday
			if (StartDate.DayOfWeek != DayOfWeek.Sunday)
			{
				ModelState.AddModelError("StartDate", "Please select a Sunday as the start date.");
				return Page();
			}

			int duplicatesSkipped = 0;

			for (int i = 0; i < NumberOfWeeks; i++)
			{
				var currentDate = StartDate.AddDays(i * 7);

				bool alreadyExists = await _context.Services.AnyAsync(s => s.Date == currentDate && s.Name == "First Service");

				if (alreadyExists)
				{
					duplicatesSkipped++;
					continue;
				}

				// 1. Create the 3 Services
				var service1 = new Service { Id = Guid.NewGuid(), Name = "First Service", Date = currentDate, StartTime = new TimeOnly(7, 30) };
				var service2 = new Service { Id = Guid.NewGuid(), Name = "Second Service", Date = currentDate, StartTime = new TimeOnly(9, 30) };
				var service3 = new Service { Id = Guid.NewGuid(), Name = "Third Service", Date = currentDate, StartTime = new TimeOnly(11, 30) };

				_context.Services.AddRange(service1, service2, service3);

				// 2. Define the 4 Default Sessions
				var defaultSessions = new List<Session>
				{
					new Session { Id = Guid.NewGuid(), Name = "Opening Prayer - Praise & Worship", Order = 1 },
					new Session { Id = Guid.NewGuid(), Name = "Prayer 1 - CCW ministration", Order = 2 },
					new Session { Id = Guid.NewGuid(), Name = "Sermon - Announcement", Order = 3 },
					new Session { Id = Guid.NewGuid(), Name = "Announcement - Benedictions", Order = 4 }
				};

				// 3. Attach the 4 sessions to EACH of the 3 services
				foreach (var session in defaultSessions)
				{
					_context.Sessions.Add(new Session
					{
						Id = Guid.NewGuid(),
						Name = session.Name,
						Order = session.Order,
						ServiceId = service1.Id
					});
					_context.Sessions.Add(new Session
					{
						Id = Guid.NewGuid(),
						Name = session.Name,
						Order = session.Order,
						ServiceId = service2.Id
					});
					_context.Sessions.Add(new Session
					{
						Id = Guid.NewGuid(),
						Name = session.Name,
						Order = session.Order,
						ServiceId = service3.Id
					});
				}
			}

			await _context.SaveChangesAsync();

			if (duplicatesSkipped > 0)
			{
				TempData["Error"] = $"Schedule generated, but {duplicatesSkipped} week(s) were skipped because they already existed in the database.";
			}
			else
			{
				TempData["Success"] = "Schedule generated successfully!";
			}

			return RedirectToPage("./Index");

		}
	}
}
