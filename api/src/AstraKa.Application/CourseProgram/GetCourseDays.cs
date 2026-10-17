using MediatR;

namespace AstraKa.Application.CourseProgram;

/// <summary>
/// A course's full base program (days + their exercises) - no visibility/authorization here, the
/// controller resolves that via <c>GetCourseByIdQuery</c> first.
/// </summary>
public sealed record GetCourseDaysQuery(Guid CourseId) : IRequest<IReadOnlyList<CourseDayWithExercisesRecord>>;

public sealed class GetCourseDaysQueryHandler(ICourseDayRepository courseDays) : IRequestHandler<GetCourseDaysQuery, IReadOnlyList<CourseDayWithExercisesRecord>>
{
    public Task<IReadOnlyList<CourseDayWithExercisesRecord>> Handle(GetCourseDaysQuery request, CancellationToken cancellationToken)
    {
        return courseDays.GetProgramByCourseIdAsync(request.CourseId, cancellationToken);
    }
}
