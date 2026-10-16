using AstraKa.Application.CourseProgram;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Tests.Ado;

[Collection("Database")]
public class CourseProgramRepositoryTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task CreateCourseWithProgramAsync_commits_the_course_and_its_full_program()
    {
        var authorId = await fixture.DbContext.Starshepherds.Select(s => s.UserId).FirstAsync();
        var resonance = await fixture.GetExerciseIdAsync("Resonance");
        var orbit = await fixture.GetExerciseIdAsync("Orbit");

        var input = new CourseProgramInput(
            authorId, "Commit Probe Course", "Description.", "Promise.", 11.11m,
            new[]
            {
                new CourseProgramDayInput(1, "Day One", null, null, new[] { new CourseProgramExerciseInput(resonance, 1, 2, 6) }),
                new CourseProgramDayInput(2, "Day Two", "Second day.", null, new[]
                {
                    new CourseProgramExerciseInput(orbit, 1, 2, 30),
                    new CourseProgramExerciseInput(resonance, 2, 2, 6),
                }),
            });

        var courseId = await fixture.CourseProgram.CreateCourseWithProgramAsync(input, CancellationToken.None);

        var courseExists = await fixture.DbContext.Courses.AnyAsync(c => c.Id == courseId && c.Name == "Commit Probe Course");
        var dayCount = await fixture.DbContext.CourseDays.CountAsync(d => d.CourseId == courseId);
        var dayExerciseCount = await fixture.DbContext.DayExercises.CountAsync(de => de.CourseDay.CourseId == courseId);

        Assert.True(courseExists);
        Assert.Equal(2, dayCount);
        Assert.Equal(3, dayExerciseCount);
    }

    [Fact]
    public async Task CreateCourseWithProgramAsync_rolls_back_everything_on_a_non_existent_exercise()
    {
        var authorId = await fixture.DbContext.Starshepherds.Select(s => s.UserId).FirstAsync();
        var realExercise = await fixture.GetExerciseIdAsync("Beacon");
        var missingExercise = Guid.NewGuid(); // guaranteed not to exist in Exercises

        var input = new CourseProgramInput(
            authorId, "Rollback Probe Course", "Description.", "Promise.", 22.22m,
            new[]
            {
                new CourseProgramDayInput(1, "Day One", null, null, new[] { new CourseProgramExerciseInput(realExercise, 1, 2, 20) }),
                new CourseProgramDayInput(2, "Day Two", null, null, new[] { new CourseProgramExerciseInput(missingExercise, 1, 2, 20) }),
            });

        await Assert.ThrowsAsync<SqlException>(() => fixture.CourseProgram.CreateCourseWithProgramAsync(input, CancellationToken.None));

        var courseExists = await fixture.DbContext.Courses.AnyAsync(c => c.Name == "Rollback Probe Course");
        Assert.False(courseExists);
    }
}
