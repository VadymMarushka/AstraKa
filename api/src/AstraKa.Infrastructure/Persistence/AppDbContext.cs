using AstraKa.Application.Common;
using AstraKa.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for everything except the course program tables, which ADO.NET owns per
/// CLAUDE.md's "ADO.NET vs EF Core boundary" (this context may still read them). Entity mapping
/// lives in one <see cref="Microsoft.EntityFrameworkCore.IEntityTypeConfiguration{TEntity}"/> per
/// entity under <c>Persistence/Configurations</c>, picked up automatically below.
/// </summary>
public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Wanderer> Wanderers => Set<Wanderer>();

    public DbSet<Starshepherd> Starshepherds => Set<Starshepherd>();

    public DbSet<ZodiacSign> ZodiacSigns => Set<ZodiacSign>();

    public DbSet<Exercise> Exercises => Set<Exercise>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<CourseSign> CourseSigns => Set<CourseSign>();

    public DbSet<Cohort> Cohorts => Set<Cohort>();

    public DbSet<CourseDay> CourseDays => Set<CourseDay>();

    public DbSet<DayExercise> DayExercises => Set<DayExercise>();

    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    public DbSet<Revelation> Revelations => Set<Revelation>();

    public DbSet<DailyReport> DailyReports => Set<DailyReport>();

    public DbSet<ExerciseCompletion> ExerciseCompletions => Set<ExerciseCompletion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
