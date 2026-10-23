using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Pages
{
    public class SetupModel : PageModel
    {
		private readonly SchedulerContext _context;

		public SetupModel(SchedulerContext context)
		{
			_context = context;
		}
		[BindProperty]
		public InputModel Input { get; set; } = new();

		public class InputModel
		{
			[Required, StringLength(100), Display(Name = "Full Name")]
			public string Name { get; set; } = string.Empty;

			[Required, EmailAddress, StringLength(200), Display(Name = "Email")]
			public string Email { get; set; } = string.Empty;

			[Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password), Display(Name = "Password")]
			public string Password { get; set; } = string.Empty;

			[Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match."), Display(Name = "Confirm Password")]
			public string ConfirmPassword { get; set; } = string.Empty;
		}

		public async Task<IActionResult> OnGetAsync()
		{
			if (await AnyAdminExistsAsync())
			{
				return RedirectToPage("/Index");
			}
			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (await AnyAdminExistsAsync())
			{
				return RedirectToPage("/Index");

			}

			if (!ModelState.IsValid)
			{
				return Page();
			}

			var emailTaken = await _context.Users.AnyAsync(u => u.Email.ToLower() == Input.Email.ToLower());

			if (emailTaken)
			{
				ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Email)}", "Email is already registered.");
				return Page();
			}

			var admin = new User
			{
				Id = Guid.NewGuid(),
				Name = Input.Name.Trim(),
				Email = Input.Email.Trim(),
				PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
				IsAdmin = true
			};

			_context.Users.Add(admin);
			await _context.SaveChangesAsync();

			TempData["SetupSuccess"] = $"Admin account created for {admin.Email}.";
			return RedirectToPage("/Index");
		}

		private Task<bool> AnyAdminExistsAsync()
		{
			return _context.Users.AnyAsync(u => u.IsAdmin);
		}
    }
}
