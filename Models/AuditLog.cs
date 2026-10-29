namespace TaskSchedulerApp.Models
{
	public class AuditLog
	{
		public Guid Id { get; set; }
		public string Action { get; set; } = string.Empty; // e.g., "Added", "Modified", "Deleted"
		public string EntityName { get; set; } = string.Empty; // e.g., "User", "Assignment"
		public string Details { get; set; } = string.Empty; // e.g., "John Doe"
		public string PerformedBy { get; set; } = string.Empty;
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	}
}
