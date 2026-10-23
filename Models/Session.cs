namespace TaskSchedulerApp.Models
{
	public class Session
	{
		public Guid Id { get; set; }
		public Guid ServiceId { get; set; }

		// e.g., "Praise & Worship"
		public string Name { get; set; } = string.Empty;

		// 1, 2, 3... to enforce chronological order
		public int Order { get; set; }

		// Navigation
		public Service Service { get; set; } = null!;
		public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
	}
}
