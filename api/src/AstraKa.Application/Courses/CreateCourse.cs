using AstraKa.Application.Common;
using AstraKa.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Courses;

/// <summary>
/// Doubles as the wire DTO, same reasoning as <see cref="Exercises.CreateExerciseCommand"/>.
/// Created as <c>Draft</c> by construction; publishing is a separate step.
/// </summary>
public sealed record CreateCourseCommand(string Name, string Description, string Promise, decimal Price, IReadOnlyList<int> ZodiacSignIds) : IRequest<Guid>;

public sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
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

public sealed class CreateCourseCommandHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<CreateCourseCommand, Guid>
{
    public async Task<Guid> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        if (request.ZodiacSignIds.Count > 0)
        {
            var existingCount = await db.ZodiacSigns.CountAsync(z => request.ZodiacSignIds.Contains(z.Id), cancellationToken);
            if (existingCount != request.ZodiacSignIds.Count)
            {
                throw new NotFoundException("One or more zodiac signs do not exist.");
            }
        }

        var course = new Course(Guid.NewGuid(), currentUser.UserId!.Value, request.Name, request.Description, request.Promise, request.Price);
        course.ReplaceSigns(request.ZodiacSignIds);

        db.Courses.Add(course);
        await db.SaveChangesAsync(cancellationToken);

        return course.Id;
    }
}
