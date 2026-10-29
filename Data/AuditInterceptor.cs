using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Data
{
	public class AuditInterceptor : SaveChangesInterceptor
	{
		public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
		{
			var context = eventData.Context;
			if (context is null)
			{
				return base.SavingChanges(eventData, result);
			}

			var auditEntries = new List<AuditLog>();

			foreach (var entry in context.ChangeTracker.Entries())
			{
				// Ignore AuditLogs themselves to prevent infinite loops
				if (entry.Entity is AuditLog || entry.State == EntityState.Unchanged || entry.State == EntityState.Detached)
				{
					continue;
				}

				var action = entry.State.ToString(); // Added, Modified, Deleted

				var entityName = entry.Entity.GetType().Name;

				// Try to get a descriptive name (like the User's name or Service name)
				string details = "Unknown";
				var entity = entry.Entity;

				if (entity is User user)
				{
					details = user.Email;
				}
				else if (entity is Camera c)
				{
					details = c.Name;
				}
				else if (entity is Service s)
				{
					details = $"{s.Name} on {s.Date}";
				}
				else if (entity is Assignment a)
				{
					details = $"Assignment {a.Id}";
				}

				auditEntries.Add(new AuditLog
				{
					Id = Guid.NewGuid(),
					Action = action,
					EntityName = entityName,
					Details = details,
					Timestamp = DateTime.UtcNow
				});
			}

			if (auditEntries.Any())
			{
				context.Set<AuditLog>().AddRange(auditEntries);
			}

			return base.SavingChanges(eventData, result);
		}
	}
}
