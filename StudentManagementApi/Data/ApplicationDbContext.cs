using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Models.Entities;

namespace StudentManagementApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseRegistration> CourseRegistrations { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<Models.Entities.Task> Tasks { get; set; }
    public DbSet<TaskScore> TaskScores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Student entity
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Gender).IsRequired().HasMaxLength(10);
            //entity.Property(e => e.CreatedAt).HasDefaultValueSql();
            
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Course entity
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CourseCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CourseTitle).IsRequired().HasMaxLength(200);
            //entity.Property(e => e.CreatedAt).HasDefaultValueSql();
            
            entity.HasIndex(e => e.CourseCode).IsUnique();
        });

        // Configure CourseRegistration entity
        modelBuilder.Entity<CourseRegistration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            //entity.Property(e => e.CreatedAt).HasDefaultValueSql();

            entity.HasOne(e => e.Student)
                  .WithMany(s => s.CourseRegistrations)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Course)
                  .WithMany(c => c.CourseRegistrations)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Ensure a student can only register for a course once
            entity.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
        });

        // Configure Attendance entity
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            //entity.Property(e => e.CreatedAt).HasDefaultValueSql();

            entity.HasOne(e => e.Student)
                  .WithMany(s => s.Attendances)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Course)
                 .WithMany(s => s.Attendances)
                 .HasForeignKey(e => e.CourseId)
                 .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

        });

        // Configure Task entity
        modelBuilder.Entity<Models.Entities.Task>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.MaxObtainableScore).IsRequired();

            entity.HasOne(e => e.Course)
                  .WithMany()
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TaskScore entity
        modelBuilder.Entity<TaskScore>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Score).IsRequired();

            entity.HasOne(e => e.Task)
                  .WithMany(t => t.TaskScores)
                  .HasForeignKey(e => e.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Student)
                  .WithMany()
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Ensure a student can only have one score per task
            entity.HasIndex(e => new { e.TaskId, e.StudentId }).IsUnique();
        });
    }
}