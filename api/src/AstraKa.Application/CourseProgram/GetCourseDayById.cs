using AstraKa.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.CourseProgram;

/// <summary>
/// Application-internal - not its own HTTP route. Used by the controller to resolve the owning
/// course's <c>AuthorId</c> before the <c>AuthorOrOracle</c> check on update/delete/add-exercise,
/// the same role <c>GetCohortByIdQuery</c> plays for cohort mutations.
/// </summary>
public sealed record GetCourseDayByIdQuery(Guid Id) : IRequest<CourseDayDetailsDto>;

public sealed record CourseDayDetailsDto(Guid Id, Guid CourseId, Guid CourseAuthorId, int DayNumber, string Title, string? Description, string? MeetLink);

public sealed class GetCourseDayByIdQueryHandler(ICourseDayRepository courseDays, IAppDbContext db) : IRequestHandler<GetCourseDayByIdQuery, CourseDayDetailsDto>
{
    public async Task<CourseDayDetailsDto> Handle(GetCourseDayByIdQuery request, CancellationToken cancellationToken)
    {
        var day = await courseDays.GetByIdAsync(request.Id, cancellationToken);
        if (day is null)
        {
            throw new NotFoundException($"Course day {request.Id} does not exist.");
        }

        var authorId = await db.Courses.Where(c => c.Id == day.CourseId).Select(c => c.AuthorId).SingleAsync(cancellationToken);

        return new CourseDayDetailsDto(day.Id, day.CourseId, authorId, day.DayNumber, day.Title, day.Description, day.MeetLink);
    }
}
