namespace TaskSchedulerApp.Models
{
	public class Service
	{
		public Guid Id { get; set; }

		// e.g., "Sunday First Service", "Monday Second Service"
		public string Name { get; set; } = string.Empty;
		public DateOnly Date { get; set; }
		public TimeOnly StartTime { get; set; }

		// Navigation
		public ICollection<Session> Sessions { get; set; } = new List<Session>();
	}
	
}
