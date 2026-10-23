namespace TaskSchedulerApp.Models
{
	public class Notification
	{
		public Guid Id { get; set; }
		public Guid UserId { get; set; }
		public string Message { get; set; } = string.Empty;
		public bool IsRead { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		// Navigation
		public User User { get; set; } = null!;
	}
}
