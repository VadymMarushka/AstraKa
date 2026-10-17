using AstraKa.Application.CourseProgram;
using AstraKa.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace AstraKa.Infrastructure.Ado;

/// <summary>
/// ADO.NET CRUD for <c>CourseDays</c> - this table is ADO.NET's per CLAUDE.md's "ADO.NET vs EF Core
/// boundary". Every method opens and disposes its own connection; nothing is cached between calls.
/// </summary>
internal sealed class CourseDayRepository(string connectionString) : ICourseDayRepository
{
    public async Task<Guid> CreateAsync(Guid courseId, int dayNumber, string title, string? description, string? meetLink, CancellationToken ct)
    {
        var id = Guid.NewGuid();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(
            """
            INSERT INTO CourseDays (Id, CourseId, DayNumber, Title, Description, MeetLink)
            VALUES (@Id, @CourseId, @DayNumber, @Title, @Description, @MeetLink);
            """,
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CourseId", courseId);
        command.Parameters.AddWithValue("@DayNumber", dayNumber);
        command.Parameters.AddWithValue("@Title", title);
        command.Parameters.AddWithValue("@Description", description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@MeetLink", meetLink ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(ct);
        return id;
    }

    public async Task<CourseDayRecord?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(
            "SELECT Id, CourseId, DayNumber, Title, Description, MeetLink FROM CourseDays WHERE Id = @Id;",
            connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Read(reader) : null;
    }

    public async Task<IReadOnlyList<CourseDayRecord>> GetByCourseIdAsync(Guid courseId, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(
            "SELECT Id, CourseId, DayNumber, Title, Description, MeetLink FROM CourseDays WHERE CourseId = @CourseId ORDER BY DayNumber;",
            connection);
        command.Parameters.AddWithValue("@CourseId", courseId);

        var results = new List<CourseDayRecord>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(Read(reader));
        }

        return results;
    }

    public async Task<IReadOnlyList<CourseDayWithExercisesRecord>> GetProgramByCourseIdAsync(Guid courseId, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        // LEFT JOIN, not INNER - a meeting-only day (zero base-program exercises) must still come
        // back. RevelationId IS NULL keeps this to the base program only, per the task's scope.
        await using var command = new SqlCommand(
            """
            SELECT cd.Id AS DayId, cd.DayNumber, cd.Title, cd.Description, cd.MeetLink,
                   de.Id AS DayExerciseId, de.OrderNumber, de.Sets, de.Amount, e.Name AS ExerciseName, e.UnitId
            FROM CourseDays cd
            LEFT JOIN DayExercises de ON de.CourseDayId = cd.Id AND de.RevelationId IS NULL
            LEFT JOIN Exercises e ON e.Id = de.ExerciseId
            WHERE cd.CourseId = @CourseId
            ORDER BY cd.DayNumber, de.OrderNumber;
            """,
            connection);
        command.Parameters.AddWithValue("@CourseId", courseId);

        // The JOIN comes back as flat rows (one per day-exercise, or one bare row for a day with
        // none) - grouped back into one record per day here, in the order SQL already gave us.
        var days = new List<CourseDayWithExercisesRecord>();
        var exercisesByDayId = new Dictionary<Guid, List<ProgramExerciseRecord>>();

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dayId = reader.GetGuid(0);
            if (!exercisesByDayId.TryGetValue(dayId, out var exercises))
            {
                exercises = new List<ProgramExerciseRecord>();
                exercisesByDayId[dayId] = exercises;
                days.Add(new CourseDayWithExercisesRecord(
                    dayId,
                    reader.GetInt32(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    exercises));
            }

            if (!reader.IsDBNull(5))
            {
                exercises.Add(new ProgramExerciseRecord(
                    reader.GetGuid(5),
                    reader.GetString(9),
                    (ExerciseUnit)reader.GetByte(10),
                    reader.GetInt32(7),
                    reader.GetInt32(8),
                    reader.GetInt32(6)));
            }
        }

        return days;
    }

    public async Task<bool> UpdateAsync(Guid id, string title, string? description, string? meetLink, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(
            "UPDATE CourseDays SET Title = @Title, Description = @Description, MeetLink = @MeetLink WHERE Id = @Id;",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Title", title);
        command.Parameters.AddWithValue("@Description", description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@MeetLink", meetLink ?? (object)DBNull.Value);

        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        return rowsAffected > 0;
    }

    public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand("DELETE FROM CourseDays WHERE Id = @Id;", connection);
        command.Parameters.AddWithValue("@Id", id);

        try
        {
            var rowsAffected = await command.ExecuteNonQueryAsync(ct);
            return rowsAffected > 0 ? DeleteOutcome.Deleted : DeleteOutcome.NotFound;
        }
        catch (SqlException ex) when (ex.Number == SqlErrorNumbers.ForeignKeyConstraintViolation)
        {
            // The day (or, cascading, one of its DayExercises) still has ExerciseCompletion rows -
            // DayExercise -> ExerciseCompletion is Restrict, so the cascade from Course -> CourseDay
            // -> DayExercise stops there and the whole DELETE fails.
            return DeleteOutcome.HasDependencies;
        }
    }

    private static CourseDayRecord Read(SqlDataReader reader)
    {
        return new CourseDayRecord(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetInt32(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5));
    }
}
