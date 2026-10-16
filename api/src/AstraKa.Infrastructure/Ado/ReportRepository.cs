using AstraKa.Application.Reports;
using AstraKa.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace AstraKa.Infrastructure.Ado;

/// <summary>
/// ADO.NET read-only reports: a JOIN across the cohort/participant tables (lab 2's "JOIN of >= 3
/// tables with filters and sorting") and a GROUP BY course statistics summary (lab 2's "aggregate
/// query with GROUP BY and >= 2 aggregate functions").
/// </summary>
internal sealed class ReportRepository(string connectionString) : IReportRepository
{
    public async Task<IReadOnlyList<CohortParticipantRecord>> GetCohortParticipantsAsync(
        Guid cohortId, EnrollmentStatus? statusFilter, CohortParticipantSort sortBy, bool descending, CancellationToken ct)
    {
        // The ORDER BY column/direction come from these two fixed literal strings, picked in C#
        // from a vetted whitelist (the sortBy enum and descending bool) - never from raw user text,
        // so there is no injection surface despite SQL Server not allowing identifiers as parameters.
        var orderByColumn = sortBy switch
        {
            CohortParticipantSort.EnrolledAt => "e.EnrolledAt",
            CohortParticipantSort.PaidPrice => "e.PaidPrice",
            _ => "u.Name",
        };
        var orderByDirection = descending ? "DESC" : "ASC";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(
            $"""
            SELECT u.Name AS WandererName, u.Email, zs.Name AS ZodiacSign,
                   e.EnrolledAt, es.Name AS Status, e.PaidPrice
            FROM Enrollments e
            INNER JOIN Wanderers w ON w.UserId = e.WandererId
            INNER JOIN Users u ON u.Id = w.UserId
            INNER JOIN ZodiacSigns zs ON zs.Id = w.ZodiacSignId
            INNER JOIN EnrollmentStatuses es ON es.Id = e.StatusId
            WHERE e.CohortId = @CohortId AND (@StatusId IS NULL OR e.StatusId = @StatusId)
            ORDER BY {orderByColumn} {orderByDirection};
            """,
            connection);
        command.Parameters.AddWithValue("@CohortId", cohortId);
        command.Parameters.AddWithValue("@StatusId", statusFilter.HasValue ? (byte)statusFilter.Value : (object)DBNull.Value);

        var results = new List<CohortParticipantRecord>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new CohortParticipantRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDateTime(3),
                reader.GetString(4),
                reader.GetDecimal(5)));
        }

        return results;
    }

    public async Task<IReadOnlyList<CourseStatisticsRecord>> GetCourseStatisticsAsync(CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        // Two aggregates are pre-computed per course in their own CTEs before joining onto Courses,
        // so the Enrollments <-> DailyReports fan-out (one enrollment can have many daily reports)
        // can never inflate EnrollmentCount/TotalRevenue - a single flat GROUP BY with both joins
        // inline would silently double-count enrollments once a course has more than one
        // DailyReport per enrollment.
        await using var command = new SqlCommand(
            """
            WITH EnrollmentAgg AS (
                SELECT co.CourseId, COUNT(*) AS EnrollmentCount, SUM(e.PaidPrice) AS TotalRevenue
                FROM Enrollments e
                INNER JOIN Cohorts co ON co.Id = e.CohortId
                GROUP BY co.CourseId
            ),
            HarmonyAgg AS (
                SELECT co.CourseId, AVG(CAST(dr.HarmonyLevel AS FLOAT)) AS AverageHarmonyLevel
                FROM DailyReports dr
                INNER JOIN Enrollments e ON e.WandererId = dr.WandererId AND e.CohortId = dr.CohortId
                INNER JOIN Cohorts co ON co.Id = e.CohortId
                GROUP BY co.CourseId
            )
            SELECT c.Id AS CourseId, c.Name AS CourseName,
                   ISNULL(ea.EnrollmentCount, 0) AS EnrollmentCount,
                   ISNULL(ea.TotalRevenue, 0) AS TotalRevenue,
                   ha.AverageHarmonyLevel
            FROM Courses c
            LEFT JOIN EnrollmentAgg ea ON ea.CourseId = c.Id
            LEFT JOIN HarmonyAgg ha ON ha.CourseId = c.Id
            ORDER BY EnrollmentCount DESC;
            """,
            connection);

        var results = new List<CourseStatisticsRecord>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new CourseStatisticsRecord(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetDecimal(3),
                reader.IsDBNull(4) ? null : reader.GetDouble(4)));
        }

        return results;
    }
}
