using AstraKa.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Courses;

/// <summary>No author/Oracle check here - that's the Api's resource-based <c>AuthorOrOracle</c> policy.</summary>
public sealed record PublishCourseCommand(Guid Id) : IRequest;

public sealed class PublishCourseCommandHandler(IAppDbContext db) : IRequestHandler<PublishCourseCommand>
{
    public async Task Handle(PublishCourseCommand request, CancellationToken cancellationToken)
    {
        // Include CourseDays so Course.Publish() can actually see whether there are any.
        var course = await db.Courses
            .Include(c => c.CourseDays)
            .SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException($"Course {request.Id} does not exist.");
        }

        course.Publish();

        await db.SaveChangesAsync(cancellationToken);
    }
}
