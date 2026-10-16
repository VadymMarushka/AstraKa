using AstraKa.Application.CourseProgram;
using AstraKa.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace AstraKa.Infrastructure.Ado;

/// <summary>
/// The one ADO.NET transaction over Courses + CourseDays + DayExercises per CLAUDE.md: "create
/// course with full program". One connection, one transaction, commits only if every insert
/// succeeds - including the FK from DayExercises to Exercises, which EF Core's migrations already
/// created and which ADO.NET gets for free here.
/// </summary>
internal sealed class CourseProgramRepository(string connectionString) : ICourseProgramRepository
{
    public async Task<Guid> CreateCourseWithProgramAsync(CourseProgramInput input, CancellationToken ct)
    {
        var courseId = Guid.NewGuid();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);

        try
        {
            await using (var courseCommand = new SqlCommand(
                """
                INSERT INTO Courses (Id, AuthorId, Name, Description, Promise, Price, StatusId)
                VALUES (@Id, @AuthorId, @Name, @Description, @Promise, @Price, @StatusId);
                """,
                connection, transaction))
            {
                courseCommand.Parameters.AddWithValue("@Id", courseId);
                courseCommand.Parameters.AddWithValue("@AuthorId", input.AuthorId);
                courseCommand.Parameters.AddWithValue("@Name", input.Name);
                courseCommand.Parameters.AddWithValue("@Description", input.Description);
                courseCommand.Parameters.AddWithValue("@Promise", input.Promise);
                courseCommand.Parameters.AddWithValue("@Price", input.Price);
                courseCommand.Parameters.AddWithValue("@StatusId", (byte)CourseStatus.Draft);
                await courseCommand.ExecuteNonQueryAsync(ct);
            }

            foreach (var day in input.Days)
            {
                var dayId = Guid.NewGuid();

                await using (var dayCommand = new SqlCommand(
                    """
                    INSERT INTO CourseDays (Id, CourseId, DayNumber, Title, Description, MeetLink)
                    VALUES (@Id, @CourseId, @DayNumber, @Title, @Description, @MeetLink);
                    """,
                    connection, transaction))
                {
                    dayCommand.Parameters.AddWithValue("@Id", dayId);
                    dayCommand.Parameters.AddWithValue("@CourseId", courseId);
                    dayCommand.Parameters.AddWithValue("@DayNumber", day.DayNumber);
                    dayCommand.Parameters.AddWithValue("@Title", day.Title);
                    dayCommand.Parameters.AddWithValue("@Description", day.Description ?? (object)DBNull.Value);
                    dayCommand.Parameters.AddWithValue("@MeetLink", day.MeetLink ?? (object)DBNull.Value);
                    await dayCommand.ExecuteNonQueryAsync(ct);
                }

                foreach (var exercise in day.Exercises)
                {
                    await using var exerciseCommand = new SqlCommand(
                        """
                        INSERT INTO DayExercises (Id, CourseDayId, ExerciseId, RevelationId, OrderNumber, Sets, Amount)
                        VALUES (@Id, @CourseDayId, @ExerciseId, NULL, @OrderNumber, @Sets, @Amount);
                        """,
                        connection, transaction);
                    exerciseCommand.Parameters.AddWithValue("@Id", Guid.NewGuid());
                    exerciseCommand.Parameters.AddWithValue("@CourseDayId", dayId);
                    exerciseCommand.Parameters.AddWithValue("@ExerciseId", exercise.ExerciseId);
                    exerciseCommand.Parameters.AddWithValue("@OrderNumber", exercise.OrderNumber);
                    exerciseCommand.Parameters.AddWithValue("@Sets", exercise.Sets);
                    exerciseCommand.Parameters.AddWithValue("@Amount", exercise.Amount);

                    // A non-existent ExerciseId violates the FK EF Core's migrations already
                    // created on DayExercises.ExerciseId -> Exercises.Id; this throws before any
                    // manual check is needed, and is exactly the rollback trigger below.
                    await exerciseCommand.ExecuteNonQueryAsync(ct);
                }
            }

            await transaction.CommitAsync(ct);
            return courseId;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
