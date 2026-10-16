using AstraKa.Application.Common;
using AstraKa.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Courses;

/// <summary>
/// Course details - 404 if missing. Also used by the controller for PUT/DELETE/publish to resolve
/// <c>AuthorId</c> before the resource-based authorization check (same shape as
/// <c>GetExerciseByIdQuery</c>). A draft requested by anyone except its author or Oracle gets the
/// same 404 - that's visibility, not authorization-to-mutate, so it's decided here with
/// <see cref="ICurrentUser"/> rather than in Api.
/// </summary>
public sealed record GetCourseByIdQuery(Guid Id) : IRequest<CourseDetailsDto>;

public sealed record CourseDetailsDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string Name,
    string Description,
    string Promise,
    decimal Price,
    CourseStatus Status,
    IReadOnlyList<string> OpenToSignNames,
    int DayCount);

public sealed class GetCourseByIdQueryHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetCourseByIdQuery, CourseDetailsDto>
{
    public async Task<CourseDetailsDto> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        var course = await db.Courses
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new CourseDetailsDto(
                c.Id,
                c.AuthorId,
                c.Author.User.Name,
                c.Name,
                c.Description,
                c.Promise,
                c.Price,
                c.Status,
                c.CourseSigns.Select(cs => cs.ZodiacSign.Name).ToList(),
                c.CourseDays.Count))
            .SingleOrDefaultAsync(cancellationToken);

        if (course is null)
        {
            throw new NotFoundException($"Course {request.Id} does not exist.");
        }

        var isVisible = course.Status == CourseStatus.Published
            || course.AuthorId == currentUser.UserId
            || currentUser.Role == UserRole.Oracle;

        if (!isVisible)
        {
            throw new NotFoundException($"Course {request.Id} does not exist.");
        }

        return course;
    }
}
