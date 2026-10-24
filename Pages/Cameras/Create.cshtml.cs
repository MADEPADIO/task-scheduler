using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages.Cameras
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
		public Camera Camera { get; set; } = default!;

		public IActionResult OnGet()
		{
			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid || _context.Cameras == null || Camera == null)
			{
				return Page();
			}
			_context.Cameras.Add(Camera);
			await _context.SaveChangesAsync();
			return RedirectToPage("./Index");
		}
	}
}
