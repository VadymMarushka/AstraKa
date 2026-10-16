using AstraKa.Tests.Common;
using AstraKa.Application.CourseProgram;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Tests.Ado;

[Collection("Database")]
public class CourseDayRepositoryTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task CreateAsync_then_GetByIdAsync_returns_the_created_day()
    {
        var courseId = await fixture.CreateThrowawayCourseAsync("CRUD Course - create/get");

        var id = await fixture.CourseDays.CreateAsync(courseId, 1, "Day One", "A description.", "https://meet.astraka.app/x", CancellationToken.None);
        var record = await fixture.CourseDays.GetByIdAsync(id, CancellationToken.None);

        Assert.NotNull(record);
        Assert.Equal(id, record!.Id);
        Assert.Equal(courseId, record.CourseId);
        Assert.Equal(1, record.DayNumber);
        Assert.Equal("Day One", record.Title);
        Assert.Equal("A description.", record.Description);
        Assert.Equal("https://meet.astraka.app/x", record.MeetLink);
    }

    [Fact]
    public async Task CreateAsync_with_null_description_and_meet_link_round_trips_as_null()
    {
        var courseId = await fixture.CreateThrowawayCourseAsync("CRUD Course - nulls");

        var id = await fixture.CourseDays.CreateAsync(courseId, 1, "Meeting-only day", null, null, CancellationToken.None);
        var record = await fixture.CourseDays.GetByIdAsync(id, CancellationToken.None);

        Assert.NotNull(record);
        Assert.Null(record!.Description);
        Assert.Null(record.MeetLink);
    }

    [Fact]
    public async Task GetByCourseIdAsync_returns_all_days_ordered_by_day_number()
    {
        var courseId = await fixture.CreateThrowawayCourseAsync("CRUD Course - list");
        await fixture.CourseDays.CreateAsync(courseId, 2, "Day Two", null, null, CancellationToken.None);
        await fixture.CourseDays.CreateAsync(courseId, 1, "Day One", null, null, CancellationToken.None);

        var days = await fixture.CourseDays.GetByCourseIdAsync(courseId, CancellationToken.None);

        Assert.Equal(2, days.Count);
        Assert.Equal(1, days[0].DayNumber);
        Assert.Equal(2, days[1].DayNumber);
    }

    [Fact]
    public async Task UpdateAsync_changes_title_description_and_meet_link()
    {
        var courseId = await fixture.CreateThrowawayCourseAsync("CRUD Course - update");
        var id = await fixture.CourseDays.CreateAsync(courseId, 1, "Original title", "Original description.", null, CancellationToken.None);

        var updated = await fixture.CourseDays.UpdateAsync(id, "New title", "New description.", "https://meet.astraka.app/y", CancellationToken.None);
        var record = await fixture.CourseDays.GetByIdAsync(id, CancellationToken.None);

        Assert.True(updated);
        Assert.Equal("New title", record!.Title);
        Assert.Equal("New description.", record.Description);
        Assert.Equal("https://meet.astraka.app/y", record.MeetLink);
    }

    [Fact]
    public async Task UpdateAsync_on_a_missing_id_returns_false()
    {
        var updated = await fixture.CourseDays.UpdateAsync(Guid.NewGuid(), "Does not exist", null, null, CancellationToken.None);

        Assert.False(updated);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_day_and_a_later_lookup_returns_null()
    {
        var courseId = await fixture.CreateThrowawayCourseAsync("CRUD Course - delete");
        var id = await fixture.CourseDays.CreateAsync(courseId, 1, "To be deleted", null, null, CancellationToken.None);

        var outcome = await fixture.CourseDays.DeleteAsync(id, CancellationToken.None);
        var record = await fixture.CourseDays.GetByIdAsync(id, CancellationToken.None);

        Assert.Equal(DeleteOutcome.Deleted, outcome);
        Assert.Null(record);
    }

    [Fact]
    public async Task DeleteAsync_on_a_missing_id_returns_NotFound()
    {
        var outcome = await fixture.CourseDays.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(DeleteOutcome.NotFound, outcome);
    }

    [Fact]
    public async Task DeleteAsync_on_a_day_whose_exercises_already_have_completions_returns_HasDependencies()
    {
        // HOMEWARD's day 1 is seeded with completed exercises for several wanderers in Cohort A.
        var homewardCourseId = await fixture.DbContext.Courses.Where(c => c.Name == "HOMEWARD").Select(c => c.Id).SingleAsync();
        var day1Id = await fixture.DbContext.CourseDays
            .Where(d => d.CourseId == homewardCourseId && d.DayNumber == 1)
            .Select(d => d.Id)
            .SingleAsync();

        var outcome = await fixture.CourseDays.DeleteAsync(day1Id, CancellationToken.None);
        var stillThere = await fixture.CourseDays.GetByIdAsync(day1Id, CancellationToken.None);

        Assert.Equal(DeleteOutcome.HasDependencies, outcome);
        Assert.NotNull(stillThere);
    }
}
