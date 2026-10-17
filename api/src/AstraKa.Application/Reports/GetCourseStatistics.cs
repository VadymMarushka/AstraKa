using AstraKa.Application.Common;
using MediatR;

namespace AstraKa.Application.Reports;

/// <summary>One course's statistics - no visibility/authorization here, the controller resolves that via <c>GetCourseByIdQuery</c> first.</summary>
public sealed record GetCourseStatisticsQuery(Guid CourseId) : IRequest<CourseStatisticsRecord>;

public sealed class GetCourseStatisticsQueryHandler(IReportRepository reports) : IRequestHandler<GetCourseStatisticsQuery, CourseStatisticsRecord>
{
    public async Task<CourseStatisticsRecord> Handle(GetCourseStatisticsQuery request, CancellationToken cancellationToken)
    {
        var statistics = await reports.GetCourseStatisticsAsync(request.CourseId, cancellationToken);
        var statistic = statistics.SingleOrDefault();

        if (statistic is null)
        {
            throw new NotFoundException($"Course {request.CourseId} does not exist.");
        }

        return statistic;
    }
}
