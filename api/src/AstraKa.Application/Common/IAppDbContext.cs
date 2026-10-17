using AstraKa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AstraKa.Application.Common;

/// <summary>
/// The persistence seam handlers use directly - no generic repository, no Unit of Work wrapper,
/// `DbContext` already is both. Implemented by <c>AppDbContext</c> in Infrastructure.
/// </summary>
public interface IAppDbContext
{
    /// <summary>
    /// EF Core's own transaction API, not a new abstraction - forwards <c>DbContext.Database</c>,
    /// which <c>AppDbContext</c> already has for free. Lets a handler open an explicit
    /// transaction (<c>Database.BeginTransactionAsync</c>) when a business operation needs one
    /// made visible, without a Unit-of-Work wrapper type.
    /// </summary>
    DatabaseFacade Database { get; }

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
