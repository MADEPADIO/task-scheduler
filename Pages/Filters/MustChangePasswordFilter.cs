using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace TaskSchedulerApp.Pages.Filters
{
	public class MustChangePasswordFilter : IAsyncPageFilter
	{
		public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

		public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
		{
			var user = context.HttpContext.User;

			// If user is logged in, and has the flag, and is NOT on the ChangePassword or Logout Page
			if (user.Identity?.IsAuthenticated == true && !context.ActionDescriptor.ViewEnginePath.Contains("/Account/ChangePassword") && !context.ActionDescriptor.ViewEnginePath.Contains("/Account/Logout"))
			{
				context.Result = new RedirectToPageResult("/Account/ChangePassword");
				return;
			}

			await next();
		}
	}
}
