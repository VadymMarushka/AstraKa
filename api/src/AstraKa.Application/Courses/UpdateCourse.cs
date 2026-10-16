using AstraKa.Application.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Courses;

/// <summary>
/// The id comes from the route, not the body - the controller merges it with the body-only
/// <c>UpdateCourseRequest</c>. No author/Oracle check here; that's the Api's resource-based
/// <c>AuthorOrOracle</c> policy, run before this is ever sent.
/// </summary>
public sealed record UpdateCourseCommand(Guid Id, string Name, string Description, string Promise, decimal Price, IReadOnlyList<int> ZodiacSignIds) : IRequest;

public sealed class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Promise).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ZodiacSignIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Zodiac sign ids must be distinct.");
    }
}

public sealed class UpdateCourseCommandHandler(IAppDbContext db) : IRequestHandler<UpdateCourseCommand>
{
    public async Task Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
    {
        // Include CourseSigns so ReplaceSigns's diff sees the current rows - otherwise an
        // un-loaded collection looks empty and nothing gets removed.
        var course = await db.Courses
            .Include(c => c.CourseSigns)
            .SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException($"Course {request.Id} does not exist.");
        }

        if (request.ZodiacSignIds.Count > 0)
        {
            var existingCount = await db.ZodiacSigns.CountAsync(z => request.ZodiacSignIds.Contains(z.Id), cancellationToken);
            if (existingCount != request.ZodiacSignIds.Count)
            {
                throw new NotFoundException("One or more zodiac signs do not exist.");
            }
        }

        course.Update(request.Name, request.Description, request.Promise, request.Price);
        course.ReplaceSigns(request.ZodiacSignIds);

        await db.SaveChangesAsync(cancellationToken);
    }
}
