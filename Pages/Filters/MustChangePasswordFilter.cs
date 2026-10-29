using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TaskSchedulerApp.Pages.Filters
{
	public class MustChangePasswordFilter : IPageFilter
	{
		public void OnPageHandlerSelected(PageHandlerSelectedContext context) { }

		public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
		{
			var user = context.HttpContext.User;

			// Check if logged in, check if the claim is "true", and ensure we aren't on the ChangePassword or Logout pages
			if (user.Identity?.IsAuthenticated == true &&
				user.FindFirst("MustChangePassword")?.Value == "true")
			{
				var path = context.ActionDescriptor.ViewEnginePath;

				// If we are NOT on the ChangePassword page, and NOT on the Logout page, redirect.
				if (!path.Contains("/Account/ChangePassword", StringComparison.OrdinalIgnoreCase) &&
					!path.Contains("/Account/Logout", StringComparison.OrdinalIgnoreCase))
				{
					context.Result = new RedirectToPageResult("/Account/ChangePassword");
				}
			}
		}

		public void OnPageHandlerExecuted(PageHandlerExecutedContext context) { }
	}
}