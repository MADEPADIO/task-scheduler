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

			[DataType(DataType.Password)]
			public string? Password { get; set; } = string.Empty;
		}

		public IActionResult OnGet()
		{
			if (User.Identity?.IsAuthenticated == true)
			{
				if (User.IsInRole("Admin"))
				{
					return RedirectToPage("/Index");
				}
				else
				{
					return RedirectToPage("/Member/Index");
				}
			}

			return Page();
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
				if (string.IsNullOrEmpty(Input.Password) || string.IsNullOrEmpty(user.PasswordHash) ||
					!BCrypt.Net.BCrypt.Verify(Input.Password, user.PasswordHash))
				{
					ModelState.AddModelError(string.Empty, "Invalid login attempt. Password is required for admins.");
					return Page();
				}
			}
			// Members just log in with their email. No password check.

			// Create the user's identity
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Name, user.Name),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "Member")
			};

			var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

			//var authProperties = new AuthenticationProperties
			//{
			//	IsPersistent = true,
			//	ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
			//};

			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

			if (user.IsAdmin)
			{
				return RedirectToPage("/Index");
			}
			else
			{
				return RedirectToPage("/Member/Index");
			}
		}

		// This method is called by Javascript to check if an email belongs to an admin
		public async Task<IActionResult> OnGetCheckAdminAsync(string email)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				return new JsonResult(new { isAdmin = false });
			}
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

			return new JsonResult(new { isAdmin = user?.IsAdmin ?? false });
		}
	}
}
