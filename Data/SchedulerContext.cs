using Microsoft.EntityFrameworkCore;
using TaskSchedulerApp.Models;

namespace TaskSchedulerApp.Data
{
	public class SchedulerContext : DbContext
	{
		public SchedulerContext(DbContextOptions<SchedulerContext> options) : base(options) { }

		public DbSet<User> Users => Set<User>();
		public DbSet<Camera> Cameras => Set<Camera>();
		public DbSet<Service> Services => Set<Service>();
		public DbSet<Session> Sessions => Set<Session>();
		public DbSet<Assignment> Assignments => Set<Assignment>();
		public DbSet<Notification> Notifications => Set<Notification>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// ----- User -----
			modelBuilder.Entity<User>(entity =>
			{
				entity.HasKey(u => u.Id);
				entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
				entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
				entity.HasIndex(u => u.Email).IsUnique();
				entity.Property(u => u.IsAdmin).HasDefaultValue(false);
				entity.Property(u => u.IsActive).HasDefaultValue(true);
				entity.Property(u => u.Notes).HasMaxLength(1000);
			});

			// ----- Camera -----
			modelBuilder.Entity<Camera>(entity =>
			{
				entity.HasKey(c => c.Id);
				entity.Property(c => c.Name).IsRequired().HasMaxLength(50);
				entity.Property(c => c.IsActive).HasDefaultValue(true);
			});

			// ----- Service -----
			modelBuilder.Entity<Service>(entity =>
			{
				entity.HasKey(s => s.Id);
				entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
				entity.Property(s => s.Date).HasColumnType("date");
				entity.Property(s => s.StartTime).HasColumnType("time");
			});

			// ----- Session -----
			modelBuilder.Entity<Session>(entity =>
			{
				entity.HasKey(s => s.Id);
				entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
				entity.Property(s => s.Order).IsRequired();

				entity.HasOne(s => s.Service)
					  .WithMany(sv => sv.Sessions)
					  .HasForeignKey(s => s.ServiceId)
					  .OnDelete(DeleteBehavior.Cascade);
			});

			// ----- Assignment -----
			modelBuilder.Entity<Assignment>(entity =>
			{
				entity.HasKey(a => a.Id);

				entity.Property(a => a.Status)
					  .HasColumnType("xid")
					  .ValueGeneratedOnAddOrUpdate()
					  .IsConcurrencyToken();

				entity.Property(a => a.AbsenceReason).HasMaxLength(500);

				// Map the shadow property "xmin" as a row version for concurrency control
				entity.Property(a => a.Status)
					  .HasConversion<string>()
					  .HasMaxLength(20)
					  .HasDefaultValue(AssignmentStatus.Pending);

				// Three explicit FKs — do not rely on convention here
				entity.HasOne(a => a.Session)
					  .WithMany(s => s.Assignments)
					  .HasForeignKey(a => a.SessionId)
					  .OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(a => a.Camera)
					  .WithMany(c => c.Assignments)
					  .HasForeignKey(a => a.CameraId)
					  .OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(a => a.User)
					  .WithMany(u => u.Assignments)
					  .HasForeignKey(a => a.UserId)
					  .OnDelete(DeleteBehavior.Cascade);

				// Hard invariants — enforced at DB level
				entity.HasIndex(a => new { a.SessionId, a.CameraId })
					  .IsUnique()
					  .HasDatabaseName("UX_Assignments_Session_Camera");

				entity.HasIndex(a => new { a.SessionId, a.UserId })
					  .IsUnique()
					  .HasDatabaseName("UX_Assignments_Session_User");
			});

			// ----- Notification -----
			modelBuilder.Entity<Notification>(entity =>
			{
				entity.HasKey(n => n.Id);
				entity.Property(n => n.Message).IsRequired().HasMaxLength(500);
				entity.Property(n => n.IsRead).HasDefaultValue(false);
				entity.Property(n => n.CreatedAt)
					  .HasDefaultValueSql("CURRENT_TIMESTAMP");

				entity.HasOne(n => n.User)
					  .WithMany(u => u.Notifications)
					  .HasForeignKey(n => n.UserId)
					  .OnDelete(DeleteBehavior.Cascade);
			});
		}
	}
}