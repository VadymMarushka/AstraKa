using AstraKa.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Exercises;

/// <summary>
/// One exercise by id - 404 if missing. Also used by the controller for PUT/DELETE to resolve
/// <c>AuthorId</c> before the resource-based authorization check, so there is one lookup path and
/// one 404 behavior for "this exercise doesn't exist".
/// </summary>
public sealed record GetExerciseByIdQuery(Guid Id) : IRequest<ExerciseDto>;

public sealed class GetExerciseByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetExerciseByIdQuery, ExerciseDto>
{
    public async Task<ExerciseDto> Handle(GetExerciseByIdQuery request, CancellationToken cancellationToken)
    {
        var exercise = await db.Exercises.AsNoTracking().SingleOrDefaultAsync(e => e.Id == request.Id, cancellationToken);
        if (exercise is null)
        {
            throw new NotFoundException($"Exercise {request.Id} does not exist.");
        }

        return new ExerciseDto(exercise.Id, exercise.AuthorId, exercise.Name, exercise.Description, exercise.Instructions, exercise.Unit, exercise.VideoUrl, exercise.AuthorId is null);
    }
}
