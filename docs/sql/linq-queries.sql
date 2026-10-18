-- EF Core LINQ queries, captured via IQueryable.ToQueryString() against a real migrated/seeded
-- AstraKa_Tests database (see tests/AstraKa.Tests/Linq/QueryTranslationTests.cs). The test calls
-- the handlers' own internal static query-building methods (GetExercisesQueryHandler.
-- BuildFilteredQuery/BuildPagedQuery and the equivalents on GetCoursesQueryHandler and
-- GetHarmonyTrendQueryHandler) with realistic filter/sort/pagination values - so what follows is
-- each handler's real SQL shape, not a simplified stand-in, and proves the LINQ translates to a
-- single server-side SQL statement rather than falling back to client-side evaluation.

-- ============================================================================================
-- Exercise list
-- Endpoint: GET /api/v1/exercises?unit=Reps&nameSearch=Stretch&page=2&pageSize=10
-- Handler:  AstraKa.Application.Exercises.GetExercisesQueryHandler (BuildFilteredQuery + BuildPagedQuery)
-- ============================================================================================
DECLARE @unit tinyint = CAST(1 AS tinyint);
DECLARE @request_NameSearch_contains nvarchar(100) = N'%Stretch%';
DECLARE @p int = 10;

SELECT [e].[Id], [e].[AuthorId], [e].[Name], [e].[Description], [e].[Instructions], [e].[UnitId], [e].[VideoUrl], CASE
    WHEN [e].[AuthorId] IS NULL THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END
FROM [Exercises] AS [e]
WHERE [e].[UnitId] = @unit AND [e].[Name] LIKE @request_NameSearch_contains ESCAPE N'\'
ORDER BY [e].[Name]
OFFSET @p ROWS FETCH NEXT @p ROWS ONLY

-- ============================================================================================
-- Course list
-- Endpoint: GET /api/v1/courses?signId=1&nameSearch=Homeward&minPrice=5&maxPrice=50&page=1&pageSize=10
-- Handler:  AstraKa.Application.Courses.GetCoursesQueryHandler (BuildFilteredQuery + BuildPagedQuery)
-- Caller is a non-Oracle, non-"mine" request, so only Published courses are visible - the
-- StatusId filter below. Pagination (OFFSET/FETCH) is pushed into the inner subquery; the author
-- and zodiac-sign joins needed for the projection are applied to the already-paged 10 rows, not
-- to the whole table.
-- ============================================================================================
DECLARE @signId int = 1;
DECLARE @request_NameSearch_contains nvarchar(150) = N'%Homeward%';
DECLARE @minPrice decimal(10,2) = 5.0;
DECLARE @maxPrice decimal(10,2) = 50.0;
DECLARE @p int = 0;
DECLARE @p5 int = 10;

SELECT [c2].[Id], [c2].[Name], [u].[Name], [s].[UserId], [u].[Id], [s0].[Name], [s0].[CourseId], [s0].[ZodiacSignId], [s0].[Id], (
    SELECT COUNT(*)
    FROM [CourseDays] AS [c4]
    WHERE [c2].[Id] = [c4].[CourseId]), [c2].[StatusId], [c2].[Price]
FROM (
    SELECT [c].[Id], [c].[AuthorId], [c].[Name], [c].[Price], [c].[StatusId]
    FROM [Courses] AS [c]
    WHERE [c].[StatusId] = CAST(2 AS tinyint) AND (NOT EXISTS (
        SELECT 1
        FROM [CourseSigns] AS [c0]
        WHERE [c].[Id] = [c0].[CourseId]) OR EXISTS (
        SELECT 1
        FROM [CourseSigns] AS [c1]
        WHERE [c].[Id] = [c1].[CourseId] AND [c1].[ZodiacSignId] = @signId)) AND [c].[Name] LIKE @request_NameSearch_contains ESCAPE N'\' AND [c].[Price] >= @minPrice AND [c].[Price] <= @maxPrice
    ORDER BY [c].[Name]
    OFFSET @p ROWS FETCH NEXT @p5 ROWS ONLY
) AS [c2]
INNER JOIN [Starshepherds] AS [s] ON [c2].[AuthorId] = [s].[UserId]
INNER JOIN [Users] AS [u] ON [s].[UserId] = [u].[Id]
LEFT JOIN (
    SELECT [z].[Name], [c3].[CourseId], [c3].[ZodiacSignId], [z].[Id]
    FROM [CourseSigns] AS [c3]
    INNER JOIN [ZodiacSigns] AS [z] ON [c3].[ZodiacSignId] = [z].[Id]
) AS [s0] ON [c2].[Id] = [s0].[CourseId]
ORDER BY [c2].[Name], [c2].[Id], [s].[UserId], [u].[Id], [s0].[CourseId], [s0].[ZodiacSignId]

-- ============================================================================================
-- Harmony trend - report stats half (average harmony level + report count, per calendar date)
-- Endpoint: GET /api/v1/cohorts/{cohortId}/harmony-trend
-- Handler:  AstraKa.Application.Reports.GetHarmonyTrendQueryHandler (BuildReportStatsQuery)
-- See docs/decisions.md ADR #13: this is deliberately a separate query from the one below -
-- EF Core cannot translate aggregating a joined/computed value after a GroupBy, so the
-- completed-exercise count is computed by its own independent GroupBy and merged in memory.
-- Both halves are scoped to one cohort (@cohortId below is an example value).
-- ============================================================================================
DECLARE @cohortId uniqueidentifier = '25394604-2969-4ee5-8743-9e04df7b6578';

SELECT [d].[Date], AVG(CAST([d].[HarmonyLevel] AS float)), COUNT(*)
FROM [DailyReports] AS [d]
WHERE [d].[CohortId] = @cohortId
GROUP BY [d].[Date]

-- ============================================================================================
-- Harmony trend - completed-exercise-count half (per calendar date)
-- Endpoint: GET /api/v1/cohorts/{cohortId}/harmony-trend
-- Handler:  AstraKa.Application.Reports.GetHarmonyTrendQueryHandler (BuildCompletionCountsQuery)
-- ============================================================================================
DECLARE @cohortId uniqueidentifier = '25394604-2969-4ee5-8743-9e04df7b6578';

SELECT [d].[Date], COUNT(*)
FROM [ExerciseCompletions] AS [e]
INNER JOIN [DailyReports] AS [d] ON [e].[DailyReportId] = [d].[Id]
WHERE [d].[CohortId] = @cohortId
GROUP BY [d].[Date]
