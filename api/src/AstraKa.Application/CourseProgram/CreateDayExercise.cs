using FluentValidation;
using MediatR;

namespace AstraKa.Application.CourseProgram;

/// <summary>
/// No author/Oracle check here - that's the Api's resource-based <c>AuthorOrOracle</c> policy.
/// Always base program - the repository call hardcodes a null RevelationId. A bad ExerciseId is
/// caught by the FK from DayExercises.ExerciseId to Exercises.Id, surfacing as a raw SqlException
/// that <c>GlobalExceptionHandler</c> maps to 409.
/// </summary>
public sealed record CreateDayExerciseCommand(Guid DayId, Guid ExerciseId, int OrderNumber, int Sets, int Amount) : IRequest<Guid>;

public sealed class CreateDayExerciseCommandValidator : AbstractValidator<CreateDayExerciseCommand>
{
    public CreateDayExerciseCommandValidator()
    {
        RuleFor(x => x.OrderNumber).GreaterThan(0);
        RuleFor(x => x.Sets).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class CreateDayExerciseCommandHandler(IDayExerciseRepository dayExercises) : IRequestHandler<CreateDayExerciseCommand, Guid>
{
    public Task<Guid> Handle(CreateDayExerciseCommand request, CancellationToken cancellationToken)
    {
        return dayExercises.CreateAsync(request.DayId, request.ExerciseId, request.OrderNumber, request.Sets, request.Amount, revelationId: null, cancellationToken);
    }
}
