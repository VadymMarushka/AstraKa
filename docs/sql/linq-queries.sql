-- EF Core LINQ queries, captured via IQueryable.ToQueryString() against a real migrated/seeded
-- AstraKa_Tests database (see tests/AstraKa.Tests/Linq/QueryTranslationTests.cs). Each one proves
-- the corresponding handler's LINQ translates to a single server-side SQL statement rather than
-- falling back to client-side evaluation.

-- ============================================================================================
-- Exercise list
-- Endpoint: GET /api/v1/exercises
-- Handler:  AstraKa.Application.Exercises.GetExercisesQueryHandler
-- ============================================================================================
SELECT [e].[Id], [e].[Name], [e].[UnitId] AS [Unit], [e].[AuthorId]
FROM [Exercises] AS [e]
WHERE [e].[UnitId] = CAST(1 AS tinyint)
ORDER BY [e].[Name]

-- ============================================================================================
-- Course list
-- Endpoint: GET /api/v1/courses
-- Handler:  AstraKa.Application.Courses.GetCoursesQueryHandler
-- ============================================================================================
SELECT [c].[Id], [c].[Name], [c].[Price], (
    SELECT COUNT(*)
    FROM [CourseDays] AS [c0]
    WHERE [c].[Id] = [c0].[CourseId]) AS [DayCount]
FROM [Courses] AS [c]
WHERE [c].[StatusId] = CAST(2 AS tinyint)
ORDER BY [c].[Name]

-- ============================================================================================
-- Harmony trend - report stats half (average harmony level + report count, per calendar date)
-- Endpoint: GET /api/v1/cohorts/{cohortId}/harmony-trend
-- Handler:  AstraKa.Application.Reports.GetHarmonyTrendQueryHandler
-- See docs/decisions.md ADR #13: this is deliberately a separate query from the one below -
-- EF Core cannot translate aggregating a joined/computed value after a GroupBy, so the
-- completed-exercise count is computed by its own independent GroupBy and merged in memory.
-- ============================================================================================
SELECT [d].[Date], AVG(CAST([d].[HarmonyLevel] AS float)) AS [Average], COUNT(*) AS [Count]
FROM [DailyReports] AS [d]
GROUP BY [d].[Date]

-- ============================================================================================
-- Harmony trend - completed-exercise-count half (per calendar date)
-- Endpoint: GET /api/v1/cohorts/{cohortId}/harmony-trend
-- Handler:  AstraKa.Application.Reports.GetHarmonyTrendQueryHandler
-- ============================================================================================
SELECT [d].[Date], COUNT(*) AS [Count]
FROM [ExerciseCompletions] AS [e]
INNER JOIN [DailyReports] AS [d] ON [e].[DailyReportId] = [d].[Id]
GROUP BY [d].[Date]
