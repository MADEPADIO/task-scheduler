using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TaskSchedulerApp.Data;

namespace TaskSchedulerApp.Pages.Account
{
    public class LoginModel : PageModel
    {
		private readonly SchedulerContext _context;

		public LoginModel(SchedulerContext context)
		{
			_context = context;
		}

		[BindProperty]
		public InputModel Input { get; set; } = new();

		public class InputModel
		{
			[Required, EmailAddress]
			public string Email { get; set; } = string.Empty;

			[Required, DataType(DataType.Password)]
			public string Password { get; set; } = string.Empty;
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

			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == Input.Email.ToLower());

			if (user is null)
			{
				ModelState.AddModelError(string.Empty, "Invalid login attempt.");
				return Page();
			}

			// If they are an admin, they MUST use a password
			if (user.IsAdmin)
			{
				if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(Input.Password, user.PasswordHash))
				{
					ModelState.AddModelError(string.Empty, "Invalid login attempt.");
					return Page();
				}

			}
			else
			{
				// NOTE: For now, we only want Admins logging in to build the roster.
				// We will add the Member passwordless login in Phase 2.
				ModelState.AddModelError(string.Empty, "Members cannot log in yet. Admin access only.");
				return Page();
			}

			// Create the user's identity
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Name, user.Name),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Role, "Admin")
			};

			var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

			//var authProperties = new AuthenticationProperties
			//{
			//	IsPersistent = true,
			//	ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
			//};

			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
			return RedirectToPage("/Index");
		}
	}
}
