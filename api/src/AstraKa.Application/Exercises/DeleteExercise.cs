using AstraKa.Application.Common;
using AstraKa.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Exercises;

/// <summary>No author/Oracle check here - that's the Api's resource-based <c>AuthorOrOracle</c> policy.</summary>
public sealed record DeleteExerciseCommand(Guid Id) : IRequest;

public sealed class DeleteExerciseCommandHandler(IAppDbContext db) : IRequestHandler<DeleteExerciseCommand>
{
    public async Task Handle(DeleteExerciseCommand request, CancellationToken cancellationToken)
    {
        var exercise = await db.Exercises.FindAsync([request.Id], cancellationToken);
        if (exercise is null)
        {
            throw new NotFoundException($"Exercise {request.Id} does not exist.");
        }

        // Explicit check, not a raw FK error - GlobalExceptionHandler's 547 case is only the
        // safety net for the race between this check and the delete below.
        var isUsedInAProgram = await db.DayExercises.AnyAsync(de => de.ExerciseId == request.Id, cancellationToken);
        if (isUsedInAProgram)
        {
            throw new DomainException("This exercise is used in a course program and cannot be deleted.");
        }

        db.Exercises.Remove(exercise);
        await db.SaveChangesAsync(cancellationToken);
    }
}
