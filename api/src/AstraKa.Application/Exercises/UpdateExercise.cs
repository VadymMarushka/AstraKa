using AstraKa.Application.Common;
using AstraKa.Domain.Enums;
using FluentValidation;
using MediatR;

namespace AstraKa.Application.Exercises;

/// <summary>
/// The id comes from the route, not the body - the controller merges it with the body-only
/// <c>UpdateExerciseRequest</c> to build this. No author/Oracle check here; that's the Api's
/// resource-based <c>AuthorOrOracle</c> policy, run before this is ever sent.
/// </summary>
public sealed record UpdateExerciseCommand(Guid Id, string Name, string Description, string Instructions, ExerciseUnit Unit, string? VideoUrl) : IRequest;

public sealed class UpdateExerciseCommandValidator : AbstractValidator<UpdateExerciseCommand>
{
    public UpdateExerciseCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Instructions).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Unit).IsInEnum();
        RuleFor(x => x.VideoUrl).MaximumLength(500).Must(BeAnAbsoluteUrl).WithMessage("VideoUrl must be an absolute URL.").When(x => x.VideoUrl is not null);
    }

    private static bool BeAnAbsoluteUrl(string? url) => Uri.TryCreate(url, UriKind.Absolute, out _);
}

public sealed class UpdateExerciseCommandHandler(IAppDbContext db) : IRequestHandler<UpdateExerciseCommand>
{
    public async Task Handle(UpdateExerciseCommand request, CancellationToken cancellationToken)
    {
        var exercise = await db.Exercises.FindAsync([request.Id], cancellationToken);
        if (exercise is null)
        {
            throw new NotFoundException($"Exercise {request.Id} does not exist.");
        }

        exercise.Update(request.Name, request.Description, request.Instructions, request.Unit, request.VideoUrl);

        await db.SaveChangesAsync(cancellationToken);
    }
}
