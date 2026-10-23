namespace TaskSchedulerApp.Models
{
	public class Camera
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;

		// Allows Admin to deactivate a camera without deleting it from the database
		public bool IsActive { get; set; }

		// Navigation
		public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
	}
}
