using AstraKa.Application.CourseProgram;
using Microsoft.Data.SqlClient;

namespace AstraKa.Infrastructure.Ado;

/// <summary>
/// ADO.NET CRUD for <c>DayExercises</c> - this table is ADO.NET's per CLAUDE.md's "ADO.NET vs EF Core
/// boundary". Every method opens and disposes its own connection; nothing is cached between calls.
/// </summary>
internal sealed class DayExerciseRepository(string connectionString) : IDayExerciseRepository
{
    public async Task<Guid> CreateAsync(Guid courseDayId, Guid exerciseId, int orderNumber, int sets, int amount, Guid? revelationId, CancellationToken ct)
    {
        var id = Guid.NewGuid();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(
            """
            INSERT INTO DayExercises (Id, CourseDayId, ExerciseId, RevelationId, OrderNumber, Sets, Amount)
            VALUES (@Id, @CourseDayId, @ExerciseId, @RevelationId, @OrderNumber, @Sets, @Amount);
            """,
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CourseDayId", courseDayId);
        command.Parameters.AddWithValue("@ExerciseId", exerciseId);
        command.Parameters.AddWithValue("@RevelationId", revelationId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OrderNumber", orderNumber);
        command.Parameters.AddWithValue("@Sets", sets);
        command.Parameters.AddWithValue("@Amount", amount);

        await command.ExecuteNonQueryAsync(ct);
        return id;
    }

    public async Task<DayExerciseRecord?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(
            "SELECT Id, CourseDayId, ExerciseId, RevelationId, OrderNumber, Sets, Amount FROM DayExercises WHERE Id = @Id;",
            connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Read(reader) : null;
    }

    public async Task<IReadOnlyList<DayExerciseRecord>> GetByCourseDayIdAsync(Guid courseDayId, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(
            "SELECT Id, CourseDayId, ExerciseId, RevelationId, OrderNumber, Sets, Amount FROM DayExercises WHERE CourseDayId = @CourseDayId ORDER BY OrderNumber;",
            connection);
        command.Parameters.AddWithValue("@CourseDayId", courseDayId);

        var results = new List<DayExerciseRecord>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(Read(reader));
        }

        return results;
    }

    public async Task<bool> UpdateAsync(Guid id, int orderNumber, int sets, int amount, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(
            "UPDATE DayExercises SET OrderNumber = @OrderNumber, Sets = @Sets, Amount = @Amount WHERE Id = @Id;",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@OrderNumber", orderNumber);
        command.Parameters.AddWithValue("@Sets", sets);
        command.Parameters.AddWithValue("@Amount", amount);

        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        return rowsAffected > 0;
    }

    public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand("DELETE FROM DayExercises WHERE Id = @Id;", connection);
        command.Parameters.AddWithValue("@Id", id);

        try
        {
            var rowsAffected = await command.ExecuteNonQueryAsync(ct);
            return rowsAffected > 0 ? DeleteOutcome.Deleted : DeleteOutcome.NotFound;
        }
        catch (SqlException ex) when (ex.Number == SqlErrorNumbers.ForeignKeyConstraintViolation)
        {
            // DayExercise -> ExerciseCompletion is Restrict: it still has completions marked against it.
            return DeleteOutcome.HasDependencies;
        }
    }

    private static DayExerciseRecord Read(SqlDataReader reader)
    {
        return new DayExerciseRecord(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.IsDBNull(3) ? null : reader.GetGuid(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetInt32(6));
    }
}
