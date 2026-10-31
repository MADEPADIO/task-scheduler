using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages.Services
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
		public Service Service { get; set; } = default!;

		public async Task<IActionResult> OnGetAsync(Guid? id)
		{
			if (id is null)
			{
				return NotFound();
			}

			var service = await _context.Services.FirstOrDefaultAsync(m => m.Id == id);
			if (service is null)
			{
				return NotFound();
			}

			Service = service;
			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			// Check for duplicates, excluding the current service being edited
			var duplicateExists = await _context.Services.AnyAsync(s => s.Id != Service.Id && s.Date == Service.Date && s.Name.ToLower() == Service.Name.ToLower());

			if (duplicateExists)
			{
				ModelState.AddModelError(string.Empty, $"Another service named '{Service.Name}' already exists on {Service.Date.ToString("MMM dd, yyyy")}.");
				return Page();
			}

			_context.Attach(Service).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!_context.Services.Any(e => e.Id == Service.Id))
				{
					return NotFound();
				}
				else throw;
			}

			return RedirectToPage("./Index");
		}
	}
}
