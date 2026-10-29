using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskSchedulerApp.Models
{
	public class Assignment
	{
		public Guid Id { get; set; }
		public Guid SessionId { get; set; }
		public int CameraId { get; set; }
		public Guid UserId { get; set; }

		public AssignmentStatus Status { get; set; } = AssignmentStatus.Pending;

		// Nullable - only populated when Status == Absent
		public string? AbsenceReason { get; set; }

		// Maps to PosgreSQL
		[Timestamp]
		public uint Version { get; set; }

		// Navigation
		public Session Session { get; set; } = null!;
		public Camera Camera { get; set; } = null!;
		public User User { get; set; } = null!;
	}
}
