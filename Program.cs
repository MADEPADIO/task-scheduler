using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TaskSchedulerApp.Data;
using TaskSchedulerApp.Pages.Filters;

namespace TaskSchedulerApp
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			var connectionString = builder.Configuration.GetConnectionString("Default");

			var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

			if (!string.IsNullOrEmpty(databaseUrl))
			{
				// Parse the URI from Railway into a standard Npgsql connection string

				var uri = new Uri(databaseUrl);
				var userInfo = uri.UserInfo.Split(':');

				var npgsqlBuilder = new NpgsqlConnectionStringBuilder
				{
					Host = uri.Host,
					Port = uri.Port,
					Username = userInfo[0],
					Password = userInfo[1],
					Database = uri.AbsolutePath.TrimStart('/'),
					SslMode = SslMode.Require
				};
				connectionString = npgsqlBuilder.ToString();
			}


			// Add services to the container.
			builder.Services.AddHttpContextAccessor();

			builder.Services.AddScoped<AuditInterceptor>();

			builder.Services.AddDbContext<SchedulerContext>((serviceProvider, options) =>
			{
				options.UseNpgsql(connectionString);
				options.AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>());
				});

			builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
				.AddCookie(options =>
				{
					options.LoginPath = "/Account/Login";
					options.LogoutPath = "/Account/Logout";
					options.AccessDeniedPath = "/Account/Login";
				});
			builder.Services.AddAuthorization();
			builder.Services.AddRazorPages();
			builder.Services.Configure<MvcOptions>(options =>
			{
				options.Filters.Add<MustChangePasswordFilter>();

			});


			var app = builder.Build();

			using (var scope = app.Services.CreateScope())
			{
				var db = scope.ServiceProvider.GetRequiredService<SchedulerContext>();
				db.Database.Migrate();
			}
			// Configure the HTTP request pipeline.
			if (!app.Environment.IsDevelopment())
			{
				// This catches 500 errors (crashes) in production
				app.UseExceptionHandler("/Error");

				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}
			else
			{
				app.UseHttpsRedirection();
			}

			// NEW: This intercepts 404 (Not Found), 403 (Access Denied), etc., 
			// and routes them to our new 404 page instead of the browser's default.
			app.UseStatusCodePagesWithReExecute("/404");

			

			app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();

			// Your .NET 10 static assets mapping
			app.MapStaticAssets();
			app.MapRazorPages()
			   .WithStaticAssets();

			app.Run();
		}
	}
}