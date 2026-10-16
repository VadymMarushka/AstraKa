using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Infrastructure;

public partial class ScaffoldedAstraKaContext : DbContext
{
    public ScaffoldedAstraKaContext(DbContextOptions<ScaffoldedAstraKaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cohort> Cohorts { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseDay> CourseDays { get; set; }

    public virtual DbSet<CourseStatus> CourseStatuses { get; set; }

    public virtual DbSet<DailyReport> DailyReports { get; set; }

    public virtual DbSet<DayExercise> DayExercises { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<EnrollmentStatus> EnrollmentStatuses { get; set; }

    public virtual DbSet<Exercise> Exercises { get; set; }

    public virtual DbSet<ExerciseCompletion> ExerciseCompletions { get; set; }

    public virtual DbSet<ExerciseUnit> ExerciseUnits { get; set; }

    public virtual DbSet<Revelation> Revelations { get; set; }

    public virtual DbSet<Starshepherd> Starshepherds { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<Wanderer> Wanderers { get; set; }

    public virtual DbSet<ZodiacSign> ZodiacSigns { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cohort>(entity =>
        {
            entity.HasIndex(e => e.CourseId, "IX_Cohorts_CourseId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Course).WithMany(p => p.Cohorts)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasIndex(e => e.AuthorId, "IX_Courses_AuthorId");

            entity.HasIndex(e => e.StatusId, "IX_Courses_StatusId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Promise).HasMaxLength(500);

            entity.HasOne(d => d.Author).WithMany(p => p.Courses)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Status).WithMany(p => p.Courses)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasMany(d => d.ZodiacSigns).WithMany(p => p.Courses)
                .UsingEntity<Dictionary<string, object>>(
                    "CourseSign",
                    r => r.HasOne<ZodiacSign>().WithMany()
                        .HasForeignKey("ZodiacSignId")
                        .OnDelete(DeleteBehavior.ClientSetNull),
                    l => l.HasOne<Course>().WithMany().HasForeignKey("CourseId"),
                    j =>
                    {
                        j.HasKey("CourseId", "ZodiacSignId");
                        j.ToTable("CourseSigns");
                        j.HasIndex(new[] { "ZodiacSignId" }, "IX_CourseSigns_ZodiacSignId");
                    });
        });

        modelBuilder.Entity<CourseDay>(entity =>
        {
            entity.HasIndex(e => new { e.CourseId, e.DayNumber }, "IX_CourseDays_CourseId_DayNumber").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.MeetLink).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(150);

            entity.HasOne(d => d.Course).WithMany(p => p.CourseDays).HasForeignKey(d => d.CourseId);
        });

        modelBuilder.Entity<CourseStatus>(entity =>
        {
            entity.HasIndex(e => e.Name, "IX_CourseStatuses_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(20);
        });

        modelBuilder.Entity<DailyReport>(entity =>
        {
            entity.HasIndex(e => new { e.WandererId, e.CohortId, e.Date }, "IX_DailyReports_WandererId_CohortId_Date").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Enrollment).WithMany(p => p.DailyReports).HasForeignKey(d => new { d.WandererId, d.CohortId });
        });

        modelBuilder.Entity<DayExercise>(entity =>
        {
            entity.HasIndex(e => new { e.CourseDayId, e.OrderNumber }, "IX_DayExercises_CourseDayId_OrderNumber")
                .IsUnique()
                .HasFilter("([RevelationId] IS NULL)");

            entity.HasIndex(e => e.ExerciseId, "IX_DayExercises_ExerciseId");

            entity.HasIndex(e => e.RevelationId, "IX_DayExercises_RevelationId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.CourseDay).WithMany(p => p.DayExercises).HasForeignKey(d => d.CourseDayId);

            entity.HasOne(d => d.Exercise).WithMany(p => p.DayExercises)
                .HasForeignKey(d => d.ExerciseId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Revelation).WithMany(p => p.DayExercises).HasForeignKey(d => d.RevelationId);
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => new { e.WandererId, e.CohortId });

            entity.HasIndex(e => e.CohortId, "IX_Enrollments_CohortId");

            entity.HasIndex(e => e.StatusId, "IX_Enrollments_StatusId");

            entity.Property(e => e.PaidPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Cohort).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.CohortId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Status).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Wanderer).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.WandererId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<EnrollmentStatus>(entity =>
        {
            entity.HasIndex(e => e.Name, "IX_EnrollmentStatuses_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(20);
        });

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.HasIndex(e => e.AuthorId, "IX_Exercises_AuthorId");

            entity.HasIndex(e => e.UnitId, "IX_Exercises_UnitId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Instructions)
                .HasMaxLength(2000)
                .HasDefaultValue("");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.VideoUrl).HasMaxLength(500);

            entity.HasOne(d => d.Author).WithMany(p => p.Exercises).HasForeignKey(d => d.AuthorId);

            entity.HasOne(d => d.Unit).WithMany(p => p.Exercises)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ExerciseCompletion>(entity =>
        {
            entity.HasIndex(e => new { e.DailyReportId, e.DayExerciseId }, "IX_ExerciseCompletions_DailyReportId_DayExerciseId").IsUnique();

            entity.HasIndex(e => e.DayExerciseId, "IX_ExerciseCompletions_DayExerciseId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.DailyReport).WithMany(p => p.ExerciseCompletions).HasForeignKey(d => d.DailyReportId);

            entity.HasOne(d => d.DayExercise).WithMany(p => p.ExerciseCompletions)
                .HasForeignKey(d => d.DayExerciseId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ExerciseUnit>(entity =>
        {
            entity.HasIndex(e => e.Name, "IX_ExerciseUnits_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(20);
        });

        modelBuilder.Entity<Revelation>(entity =>
        {
            entity.HasIndex(e => e.CourseDayId, "IX_Revelations_CourseDayId");

            entity.HasIndex(e => new { e.WandererId, e.CohortId }, "IX_Revelations_WandererId_CohortId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Message).HasMaxLength(2000);

            entity.HasOne(d => d.CourseDay).WithMany(p => p.Revelations)
                .HasForeignKey(d => d.CourseDayId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Enrollment).WithMany(p => p.Revelations).HasForeignKey(d => new { d.WandererId, d.CohortId });
        });

        modelBuilder.Entity<Starshepherd>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.Title).HasMaxLength(100);

            entity.HasOne(d => d.User).WithOne(p => p.Starshepherd).HasForeignKey<Starshepherd>(d => d.UserId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "IX_Users_Email").IsUnique();

            entity.HasIndex(e => e.ExternalId, "IX_Users_ExternalId").IsUnique();

            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.ExternalId).HasMaxLength(200);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(e => e.Name, "IX_UserRoles_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(20);
        });

        modelBuilder.Entity<Wanderer>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.HasIndex(e => e.ZodiacSignId, "IX_Wanderers_ZodiacSignId");

            entity.Property(e => e.UserId).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithOne(p => p.Wanderer).HasForeignKey<Wanderer>(d => d.UserId);

            entity.HasOne(d => d.ZodiacSign).WithMany(p => p.Wanderers)
                .HasForeignKey(d => d.ZodiacSignId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ZodiacSign>(entity =>
        {
            entity.HasIndex(e => e.Name, "IX_ZodiacSigns_Name").IsUnique();

            entity.HasIndex(e => e.SequenceNumber, "IX_ZodiacSigns_SequenceNumber").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
