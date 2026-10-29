using System.ComponentModel.DataAnnotations;

namespace TaskSchedulerApp.Models
{
	public class User
	{
		public Guid Id { get; set; }

		public string Name { get; set; } = string.Empty;

		public string Email { get; set; } = string.Empty;

		// Nullable — only populated if IsAdmin is true
		public string? PasswordHash { get; set; }
		public bool IsAdmin { get; set; }

		public bool MustChangePassword { get; set; } = false;

		// Soft delete flag
		public bool IsActive { get; set; } = true;

		// Admin-only note about capability (e.g., "Only knows handheld")
		public string? Notes { get; set; }

		// Navigation property for related tasks
		public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
		public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
	}
}
