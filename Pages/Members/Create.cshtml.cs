using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages.Members
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
		public User Member { get; set; } = default!;

        public IActionResult OnGet()
        {
			return Page();
        }

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			// Check if email is already in use
			if (await _context.Users.AnyAsync(u => u.Email.ToLower() == Member.Email.ToLower()))
			{
				ModelState.AddModelError("Member.Email", "This email is already registered.");
				return Page();
			}

			Member.Id = Guid.NewGuid();
			Member.Email = Member.Email.Trim();

			_context.Users.Add(Member);
			await _context.SaveChangesAsync();

			return RedirectToPage("./Index");
		}
	}
}
