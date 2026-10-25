using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages.Services
{
	[Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
		private readonly SchedulerContext _context;

		public DetailsModel(SchedulerContext context)
		{
			_context = context;
		}

		public Service Service { get; set; } = default!;

		[BindProperty]
		public AddSessionInput NewSession { get; set; } = new();

		public class AddSessionInput
		{
			[Required(ErrorMessage = "Session name is required.")]
			public string Name { get; set; } = string.Empty;

			[Required]
			public int Order { get; set; }

		}

		public async Task<IActionResult> OnGetAsync(Guid? id)
		{
			if (id is null)
			{
				return NotFound();
			}

			var service = await _context.Services
				.Include(s => s.Sessions
				.OrderBy(session => session.Order))
				.FirstOrDefaultAsync(m => m.Id == id);

			if (service is null)
			{
				return NotFound();
			}

			Service = service;

			// Set the default next order number for the form
			NewSession.Order = service.Sessions.Any() ? service.Sessions.Max(s => s.Order) + 1 : 1;

			return Page();
		}

		public async Task<IActionResult> OnPostAddSessionAsync(Guid id)
		{
			var service = await _context.Services.Include(s => s.Sessions).FirstOrDefaultAsync(m => m.Id == id);

			if (service is null)
			{
				return NotFound();
			}

			if (!ModelState.IsValid)
			{
				Service = service;
				return Page();
			}

			var session = new Session
			{
				Id = Guid.NewGuid(),
				ServiceId = service.Id,
				Name = NewSession.Name,
				Order = NewSession.Order
			};

			_context.Sessions.Add(session);
			await _context.SaveChangesAsync();

			return RedirectToPage("./Details", new { id = service.Id });

		}

		public async Task<IActionResult> OnPostDeleteSessionAsync(Guid sessionId, Guid serviceId)
		{
			var session = await _context.Sessions.FindAsync(sessionId);
			if (session is not null)
			{
				_context.Sessions.Remove(session);
				await _context.SaveChangesAsync();
			}

			return RedirectToPage("./Details", new { id = serviceId });

		}


    }
}
