using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
		public DateOnly SelectedDate { get; set; }

		public IActionResult OnGet()
		{
			int diff = (7 + (DayOfWeek.Sunday - DateTime.Today.DayOfWeek)) % 7;
			SelectedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(diff == 0 ? 7 : diff));
			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			// 1. Create the 3 Services
			var service1 = new Service { Id = Guid.NewGuid(), Name = "First Service", Date = SelectedDate, StartTime = new TimeOnly(8, 0) };
			var service2 = new Service { Id = Guid.NewGuid(), Name = "Second Service", Date = SelectedDate, StartTime = new TimeOnly(10, 0) };
			var service3 = new Service { Id = Guid.NewGuid(), Name = "Third Service", Date = SelectedDate, StartTime = new TimeOnly(12, 0) };

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

			await _context.SaveChangesAsync();

			return RedirectToPage("./Index");

		}
	}
}
