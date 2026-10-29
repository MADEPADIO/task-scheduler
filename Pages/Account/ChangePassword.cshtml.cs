using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TaskSchedulerApp.Data;

namespace TaskSchedulerApp.Pages.Account
{
	[Authorize]
    public class ChangePasswordModel : PageModel
    {
		private readonly SchedulerContext _context;

		public ChangePasswordModel(SchedulerContext context)
		{
			_context = context;
		}

		[BindProperty]
		public InputModel Input { get; set; } = new InputModel();

		public class InputModel
		{
			[Required, DataType(DataType.Password)]
			public string CurrentPassword { get; set; } = string.Empty;

			[Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
			public string NewPassword { get; set; } = string.Empty;

			[Required, DataType(DataType.Password), Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
			public string ConfirmPassword { get; set; } = string.Empty;
		}

		public void OnGet()
		{

		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!Guid.TryParse(userIdString, out Guid userId))
			{
				return RedirectToPage("/Account/Login");
			}

			var user = await _context.Users.FindAsync(userId);
			if (user == null)
			{
				return NotFound();
			}

			// Verify the old (temporary) password
			if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(Input.CurrentPassword, user.PasswordHash))
			{
				ModelState.AddModelError("Input.CurrentPassword", "Incorrect current password.");
				return Page();
			}

			// Update new password and remove the forced flag
			user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.NewPassword);
			user.MustChangePassword = false;
			await _context.SaveChangesAsync();

			// Update cookie to avoid being trapped on this page
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Name, user.Name),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "Member"),
				new Claim("MustChangePassword", "false")
			};

			var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

			TempData["Success"] = "Password updated succefully.";
			return user.IsAdmin ? RedirectToPage("/Index") : RedirectToPage("/Member/Index");

		}
	}
}
