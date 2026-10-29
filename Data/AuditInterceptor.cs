using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Data
{
	public class AuditInterceptor : SaveChangesInterceptor
	{
		private readonly IHttpContextAccessor _httpContextAccessor;

		public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
		{
			_httpContextAccessor = httpContextAccessor;
		}

		public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
		{
			GenerateAuditLogs(eventData.Context);
			return base.SavingChangesAsync(eventData, result, cancellationToken);
		}

		public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
		{
			GenerateAuditLogs(eventData.Context);
			return base.SavingChanges(eventData, result);
		}

		private void GenerateAuditLogs(DbContext? context)
		{
			if (context == null) return;

			var auditEntries = new List<AuditLog>();

			// NEW: Get the current user's name from the HTTP context
			var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System (Automated)";

			foreach (var entry in context.ChangeTracker.Entries())
			{
				if (entry.Entity is AuditLog || entry.State == EntityState.Unchanged || entry.State == EntityState.Detached)
					continue;

				var action = entry.State.ToString();
				var entityName = entry.Entity.GetType().Name;

				string details = "Unknown";
				var entity = entry.Entity;

				if (entity is User u) details = $"User: {u.Name} ({u.Email})";
				else if (entity is Camera c) details = $"Camera: {c.Name}";
				else if (entity is Service s) details = $"Service: {s.Name} on {s.Date}";
				else if (entity is Session se) details = $"Session: {se.Name} (Order {se.Order})";
				else if (entity is Assignment a) details = $"Assignment for Camera ID: {a.CameraId} (Status: {a.Status})";

				auditEntries.Add(new AuditLog
				{
					Id = Guid.NewGuid(),
					Action = action,
					EntityName = entityName,
					Details = details,
					PerformedBy = userName, // <-- SAVE THE USER HERE
					Timestamp = DateTime.UtcNow
				});
			}

			if (auditEntries.Any())
			{
				context.Set<AuditLog>().AddRange(auditEntries);
			}
		}
	}
}