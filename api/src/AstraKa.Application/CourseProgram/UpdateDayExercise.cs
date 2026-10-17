using AstraKa.Application.Common;
using FluentValidation;
using MediatR;

namespace AstraKa.Application.CourseProgram;

/// <summary>The id comes from the route. No author/Oracle check here - that's the Api's resource-based <c>AuthorOrOracle</c> policy.</summary>
public sealed record UpdateDayExerciseCommand(Guid Id, int OrderNumber, int Sets, int Amount) : IRequest;

public sealed class UpdateDayExerciseCommandValidator : AbstractValidator<UpdateDayExerciseCommand>
{
    public UpdateDayExerciseCommandValidator()
    {
        RuleFor(x => x.OrderNumber).GreaterThan(0);
        RuleFor(x => x.Sets).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class UpdateDayExerciseCommandHandler(IDayExerciseRepository dayExercises) : IRequestHandler<UpdateDayExerciseCommand>
{
    public async Task Handle(UpdateDayExerciseCommand request, CancellationToken cancellationToken)
    {
        var updated = await dayExercises.UpdateAsync(request.Id, request.OrderNumber, request.Sets, request.Amount, cancellationToken);
        if (!updated)
        {
            throw new NotFoundException($"Day exercise {request.Id} does not exist.");
        }
    }
}
