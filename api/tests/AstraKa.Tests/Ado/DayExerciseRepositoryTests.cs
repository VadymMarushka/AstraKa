using AstraKa.Tests.Common;
using AstraKa.Application.CourseProgram;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Tests.Ado;

[Collection("Database")]
public class DayExerciseRepositoryTests(DatabaseFixture fixture)
{
    private async Task<Guid> CreateDayAsync(string courseName)
    {
        var courseId = await fixture.CreateThrowawayCourseAsync(courseName);
        return await fixture.CourseDays.CreateAsync(courseId, 1, "Day One", null, null, CancellationToken.None);
    }

    [Fact]
    public async Task CreateAsync_then_GetByIdAsync_returns_the_created_exercise()
    {
        var dayId = await CreateDayAsync("DayExercise CRUD - create/get");
        var exerciseId = await fixture.GetExerciseIdAsync("Beacon");

        var id = await fixture.DayExercises.CreateAsync(dayId, exerciseId, 1, 2, 20, null, CancellationToken.None);
        var record = await fixture.DayExercises.GetByIdAsync(id, CancellationToken.None);

        Assert.NotNull(record);
        Assert.Equal(dayId, record!.CourseDayId);
        Assert.Equal(exerciseId, record.ExerciseId);
        Assert.Null(record.RevelationId);
        Assert.Equal(1, record.OrderNumber);
        Assert.Equal(2, record.Sets);
        Assert.Equal(20, record.Amount);
    }

    [Fact]
    public async Task GetByCourseDayIdAsync_returns_all_exercises_ordered_by_order_number()
    {
        var dayId = await CreateDayAsync("DayExercise CRUD - list");
        var beacon = await fixture.GetExerciseIdAsync("Beacon");
        var span = await fixture.GetExerciseIdAsync("Span");
        await fixture.DayExercises.CreateAsync(dayId, span, 2, 3, 30, null, CancellationToken.None);
        await fixture.DayExercises.CreateAsync(dayId, beacon, 1, 2, 20, null, CancellationToken.None);

        var exercises = await fixture.DayExercises.GetByCourseDayIdAsync(dayId, CancellationToken.None);

        Assert.Equal(2, exercises.Count);
        Assert.Equal(1, exercises[0].OrderNumber);
        Assert.Equal(2, exercises[1].OrderNumber);
    }

    [Fact]
    public async Task UpdateAsync_changes_order_sets_and_amount()
    {
        var dayId = await CreateDayAsync("DayExercise CRUD - update");
        var exerciseId = await fixture.GetExerciseIdAsync("Beacon");
        var id = await fixture.DayExercises.CreateAsync(dayId, exerciseId, 1, 2, 20, null, CancellationToken.None);

        var updated = await fixture.DayExercises.UpdateAsync(id, 3, 4, 40, CancellationToken.None);
        var record = await fixture.DayExercises.GetByIdAsync(id, CancellationToken.None);

        Assert.True(updated);
        Assert.Equal(3, record!.OrderNumber);
        Assert.Equal(4, record.Sets);
        Assert.Equal(40, record.Amount);
    }

    [Fact]
    public async Task UpdateAsync_on_a_missing_id_returns_false()
    {
        var updated = await fixture.DayExercises.UpdateAsync(Guid.NewGuid(), 1, 1, 1, CancellationToken.None);

        Assert.False(updated);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_exercise_and_a_later_lookup_returns_null()
    {
        var dayId = await CreateDayAsync("DayExercise CRUD - delete");
        var exerciseId = await fixture.GetExerciseIdAsync("Beacon");
        var id = await fixture.DayExercises.CreateAsync(dayId, exerciseId, 1, 2, 20, null, CancellationToken.None);

        var outcome = await fixture.DayExercises.DeleteAsync(id, CancellationToken.None);
        var record = await fixture.DayExercises.GetByIdAsync(id, CancellationToken.None);

        Assert.Equal(DeleteOutcome.Deleted, outcome);
        Assert.Null(record);
    }

    [Fact]
    public async Task DeleteAsync_on_a_missing_id_returns_NotFound()
    {
        var outcome = await fixture.DayExercises.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(DeleteOutcome.NotFound, outcome);
    }

    [Fact]
    public async Task DeleteAsync_on_an_exercise_that_already_has_completions_returns_HasDependencies()
    {
        // HOMEWARD's day 1 exercises (Quiet Tide, Steady Flame) were completed by several wanderers.
        var homewardCourseId = await fixture.DbContext.Courses.Where(c => c.Name == "HOMEWARD").Select(c => c.Id).SingleAsync();
        var day1Id = await fixture.DbContext.CourseDays
            .Where(d => d.CourseId == homewardCourseId && d.DayNumber == 1)
            .Select(d => d.Id)
            .SingleAsync();
        var dayExerciseId = await fixture.DbContext.DayExercises
            .Where(de => de.CourseDayId == day1Id)
            .Select(de => de.Id)
            .FirstAsync();

        var outcome = await fixture.DayExercises.DeleteAsync(dayExerciseId, CancellationToken.None);
        var stillThere = await fixture.DayExercises.GetByIdAsync(dayExerciseId, CancellationToken.None);

        Assert.Equal(DeleteOutcome.HasDependencies, outcome);
        Assert.NotNull(stillThere);
    }
}
