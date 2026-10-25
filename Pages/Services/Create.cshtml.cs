using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

			_context.Services.Add(Service);
			await _context.SaveChangesAsync();

			return RedirectToPage("./Index");

		}
	}
}
