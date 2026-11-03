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
				string details = "";

				var entity = entry.Entity;
				string entityIdentifier = "Item";

				if (entity is User u) entityIdentifier = $"User '{u.Name}'";
				else if (entity is Camera c) entityIdentifier = $"Camera '{c.Name}'";
				else if (entity is Service s) entityIdentifier = $"Service '{s.Name}' ({s.Date})";
				else if (entity is Session se) entityIdentifier = $"Session '{se.Name}'";

				if (entry.State == EntityState.Added)
				{
					details = $"Created {entityIdentifier}";
				}
				else if (entry.State == EntityState.Deleted)
				{
					details = $"Deleted {entityIdentifier}";
				}
				else if (entry.State == EntityState.Modified)
				{
					var changes = new List<string>();
					foreach (var prop in entry.Properties)
					{
						// ONLY log if the value actually changed
						if (!Equals(prop.OriginalValue, prop.CurrentValue))
						{
							string field = prop.Metadata.Name;
							string oldVal = prop.OriginalValue?.ToString() ?? "empty";
							string newVal = prop.CurrentValue?.ToString() ?? "empty";

							// Translate Booleans to English
							if (prop.OriginalValue is bool b1) oldVal = b1 ? "Yes" : "No";
							if (prop.CurrentValue is bool b2) newVal = b2 ? "Yes" : "No";

							// Format specific fields in a human-readable way
							if (field == "IsActive")
							{
								changes.Add($"Status changed to {(newVal == "Yes" ? "Active" : "Inactive")}");
							}
							else if (field == "IsAdmin")
							{
								changes.Add($"Role changed to {(newVal == "Yes" ? "Admin" : "Member")}");
							}
							else
							{
								changes.Add($"{field} changed from '{oldVal}' to '{newVal}'");
							}
						}
					}

					// If nothing actually changed, skip creating a log entry
					if (!changes.Any()) continue;

					details = $"Updated {entityIdentifier}: {string.Join(", ", changes)}";
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