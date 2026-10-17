using AstraKa.Application.Common;
using AstraKa.Domain.Exceptions;
using MediatR;

namespace AstraKa.Application.CourseProgram;

/// <summary>No author/Oracle check here - that's the Api's resource-based <c>AuthorOrOracle</c> policy.</summary>
public sealed record DeleteDayExerciseCommand(Guid Id) : IRequest;

public sealed class DeleteDayExerciseCommandHandler(IDayExerciseRepository dayExercises) : IRequestHandler<DeleteDayExerciseCommand>
{
    public async Task Handle(DeleteDayExerciseCommand request, CancellationToken cancellationToken)
    {
        var outcome = await dayExercises.DeleteAsync(request.Id, cancellationToken);

        switch (outcome)
        {
            case DeleteOutcome.NotFound:
                throw new NotFoundException($"Day exercise {request.Id} does not exist.");
            case DeleteOutcome.HasDependencies:
                throw new DomainException("This exercise has already been marked completed and cannot be deleted.");
        }
    }
}
