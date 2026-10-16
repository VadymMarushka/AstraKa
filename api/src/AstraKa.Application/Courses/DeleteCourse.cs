using AstraKa.Application.Common;
using AstraKa.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Courses;

/// <summary>No author/Oracle check here - that's the Api's resource-based <c>AuthorOrOracle</c> policy.</summary>
public sealed record DeleteCourseCommand(Guid Id) : IRequest;

public sealed class DeleteCourseCommandHandler(IAppDbContext db) : IRequestHandler<DeleteCourseCommand>
{
    public async Task Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
    {
        var course = await db.Courses.SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (course is null)
        {
            throw new NotFoundException($"Course {request.Id} does not exist.");
        }

        // Explicit check, not a raw FK error - GlobalExceptionHandler's 547 case is only the
        // safety net for the race between this check and the delete below.
        var hasCohorts = await db.Cohorts.AnyAsync(c => c.CourseId == request.Id, cancellationToken);
        if (hasCohorts)
        {
            throw new DomainException("This course has cohorts and cannot be deleted.");
        }

        // CourseDays/CourseSigns cascade at the database level - no extra code needed here.
        db.Courses.Remove(course);
        await db.SaveChangesAsync(cancellationToken);
    }
}
