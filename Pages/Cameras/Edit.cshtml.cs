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

			// Fetch the existing camera from the database
			var cameraToUpdate = await _context.Cameras.FindAsync(Camera.Id);
			if (cameraToUpdate is null)
			{
				return NotFound();
			}
			
			// Update only the fields that were submitted
			cameraToUpdate.Name = Camera.Name;
			cameraToUpdate.IsActive = Camera.IsActive;

			await _context.SaveChangesAsync();
			return RedirectToPage("./Index");
		}

		private bool CameraExists(int id)
		{
			return _context.Cameras.Any(e => e.Id == id);
		}
	}
}
