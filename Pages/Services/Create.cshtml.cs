using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages.Services
{
	[Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
		private readonly SchedulerContext _context;

		public CreateModel(SchedulerContext context)
		{
			_context = context;
		}

		[BindProperty]
		public Service Service { get; set; } = default!;

        public IActionResult OnGet()
        {
			// Default to the upcoming Sunday
			int diff = (7 + (DayOfWeek.Sunday - DateTime.Today.DayOfWeek)) % 7;
			Service = new Service
			{
				Date = DateOnly.FromDateTime(DateTime.Today.AddDays(diff == 0 ? 7 : diff)),
				StartTime = new TimeOnly(9, 0) // Default to 9:00AM
			};
			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				return Page();

			}

			// Check for duplicates (Same Date and Same Name)
			var duplicateExists = await _context.Services.AnyAsync(s => s.Date == Service.Date && s.Name.ToLower() == Service.Name.ToLower());

			if (duplicateExists)
			{
				ModelState.AddModelError(string.Empty, $"A service named '{Service.Name}' already exists on {Service.Date.ToString("MMM dd, yyyy")}.");
				return Page();
			}

			_context.Services.Add(Service);
			await _context.SaveChangesAsync();

			return RedirectToPage("./Index");

		}
	}
}
