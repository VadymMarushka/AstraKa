using AstraKa.Application.Common;
using AstraKa.Domain.Entities;
using AstraKa.Domain.Enums;
using FluentValidation;
using MediatR;

namespace AstraKa.Application.Exercises;

/// <summary>
/// Doubles as the wire DTO - bound straight from the body, same as
/// <see cref="Me.OnboardAsWandererCommand"/>. <c>AuthorId</c> is never client-supplied; the
/// handler resolves it from <see cref="ICurrentUser.UserId"/>.
/// </summary>
public sealed record CreateExerciseCommand(string Name, string Description, string Instructions, ExerciseUnit Unit, string? VideoUrl) : IRequest<Guid>;

public sealed class CreateExerciseCommandValidator : AbstractValidator<CreateExerciseCommand>
{
    public CreateExerciseCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Instructions).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Unit).IsInEnum();
        RuleFor(x => x.VideoUrl).MaximumLength(500).Must(BeAnAbsoluteUrl).WithMessage("VideoUrl must be an absolute URL.").When(x => x.VideoUrl is not null);
    }

    private static bool BeAnAbsoluteUrl(string? url) => Uri.TryCreate(url, UriKind.Absolute, out _);
}

public sealed class CreateExerciseCommandHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<CreateExerciseCommand, Guid>
{
    public async Task<Guid> Handle(CreateExerciseCommand request, CancellationToken cancellationToken)
    {
        var exercise = new Exercise(Guid.NewGuid(), currentUser.UserId, request.Name, request.Description, request.Instructions, request.Unit, request.VideoUrl);

        db.Exercises.Add(exercise);
        await db.SaveChangesAsync(cancellationToken);

        return exercise.Id;
    }
}
