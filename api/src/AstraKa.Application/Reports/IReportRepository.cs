using AstraKa.Domain.Enums;

namespace AstraKa.Application.Reports;

/// <summary>
/// ADO.NET read-only reports: a JOIN across the cohort/participant tables and a GROUP BY course
/// statistics summary. Implemented in Infrastructure with Microsoft.Data.SqlClient.
/// </summary>
public interface IReportRepository
{
    Task<IReadOnlyList<CohortParticipantRecord>> GetCohortParticipantsAsync(
        Guid cohortId, EnrollmentStatus? statusFilter, CohortParticipantSort sortBy, bool descending, CancellationToken ct);

    Task<IReadOnlyList<CourseStatisticsRecord>> GetCourseStatisticsAsync(CancellationToken ct);
}

public enum CohortParticipantSort
{
    Name,
    EnrolledAt,
    PaidPrice,
}

public sealed record CohortParticipantRecord(string WandererName, string Email, string ZodiacSign, DateTime EnrolledAt, string Status, decimal PaidPrice);

public sealed record CourseStatisticsRecord(Guid CourseId, string CourseName, int EnrollmentCount, decimal TotalRevenue, double? AverageHarmonyLevel);
