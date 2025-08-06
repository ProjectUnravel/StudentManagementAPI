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
    }
}