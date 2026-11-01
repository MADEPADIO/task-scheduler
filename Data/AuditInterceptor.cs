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
			var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

			foreach (var entry in context.ChangeTracker.Entries())
			{
				if (entry.Entity is AuditLog || entry.State == EntityState.Unchanged || entry.State == EntityState.Detached)
					continue;

				var action = entry.State.ToString();
				var entityName = entry.Entity.GetType().Name;
				string details = "Unknown";

				var entity = entry.Entity;
				string entityIdentifier = "Unknown Item";

				// 1. Get the identity of the item being changed
				if (entity is User u) entityIdentifier = $"User '{u.Name}'";
				else if (entity is Camera c) entityIdentifier = $"Camera '{c.Name}'";
				else if (entity is Service s) entityIdentifier = $"Service '{s.Name}'";
				else if (entity is Session se) entityIdentifier = $"Session '{se.Name}'";
				else if (entity is Assignment a) entityIdentifier = $"Assignment for Camera ID {a.CameraId}";

				// 2. Build the details based on the action
				if (entry.State == EntityState.Added || entry.State == EntityState.Deleted)
				{
					details = entityIdentifier;
				}
				else if (entry.State == EntityState.Modified)
				{
					var modifiedProps = new List<string>();
					foreach (var prop in entry.Properties)
					{
						if (prop.IsModified && !prop.Metadata.IsPrimaryKey())
						{
							string oldVal = prop.OriginalValue?.ToString() ?? "null";
							string newVal = prop.CurrentValue?.ToString() ?? "null";

							if (prop.OriginalValue is bool b1) oldVal = b1 ? "Yes" : "No";
							if (prop.CurrentValue is bool b2) newVal = b2 ? "Yes" : "No";

							modifiedProps.Add($"{prop.Metadata.Name}: '{oldVal}' -> '{newVal}'");
						}
					}

					if (modifiedProps.Any())
					{
						// Combine the identifier and the changes
						details = $"{entityIdentifier} | {string.Join(" | ", modifiedProps)}";
					}
				}

				auditEntries.Add(new AuditLog
				{
					Id = Guid.NewGuid(),
					Action = action,
					EntityName = entityName,
					Details = details,
					PerformedBy = userName,
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