using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages.Members
{
	[Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
		private readonly SchedulerContext _context;

		public EditModel(SchedulerContext context)
		{
			_context = context;
		}

		[BindProperty]
		public User Member { get; set; } = default!;

		public async Task<IActionResult> OnGetAsync(Guid? id)
		{
			if (id is null)
			{
				return NotFound();
			}

			var member = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
			if (member is null)
			{
				return NotFound();
			}

			Member = member;
			return Page();

		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			// Ensure email is unique if it was changed
			if (await _context.Users.AnyAsync(u => u.Email.ToLower() == Member.Email.ToLower() && u.Id != Member.Id))
			{
				ModelState.AddModelError("Member.Email", "This email is already registered to another user.");
				return Page();
			}

			var memberToUpdate = await _context.Users.FindAsync(Member.Id);
			if (memberToUpdate is null)
			{
				return NotFound();
			}

			memberToUpdate.Name = Member.Name;
			memberToUpdate.Email = Member.Email.Trim();
			memberToUpdate.Notes = Member.Notes;

			await _context.SaveChangesAsync();

			return RedirectToPage("./Index");

		}
	}
}
