using AstraKa.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Common;

/// <summary>
/// The persistence seam handlers use directly - no generic repository, no Unit of Work wrapper,
/// `DbContext` already is both. Implemented by <c>AppDbContext</c> in Infrastructure.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }

    DbSet<Wanderer> Wanderers { get; }

    DbSet<Starshepherd> Starshepherds { get; }

    DbSet<ZodiacSign> ZodiacSigns { get; }

    DbSet<Exercise> Exercises { get; }

    DbSet<Course> Courses { get; }

    DbSet<CourseSign> CourseSigns { get; }

    DbSet<Cohort> Cohorts { get; }

    DbSet<CourseDay> CourseDays { get; }

    DbSet<DayExercise> DayExercises { get; }

    DbSet<Enrollment> Enrollments { get; }

    DbSet<Revelation> Revelations { get; }

    DbSet<DailyReport> DailyReports { get; }

    DbSet<ExerciseCompletion> ExerciseCompletions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
