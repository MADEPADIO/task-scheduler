using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages.Cameras
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
		public Camera Camera { get; set; } = default!;

		public async Task<IActionResult> OnGetAsync(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var camera = await _context.Cameras.FirstOrDefaultAsync(m => m.Id == id);


			if (camera == null)
			{
				return NotFound();
			}

			Camera = camera;
			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}
			_context.Attach(Camera).State = EntityState.Modified;
			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!CameraExists(Camera.Id))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}
			return RedirectToPage("./Index");
		}

		private bool CameraExists(int id)
		{
			return _context.Cameras.Any(e => e.Id == id);
		}
	}
}
