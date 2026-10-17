using AstraKa.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.CourseProgram;

/// <summary>
/// Application-internal - not its own HTTP route. Resolves the owning course's <c>AuthorId</c>
/// (via the day-exercise's day's course) before the <c>AuthorOrOracle</c> check on update/delete.
/// </summary>
public sealed record GetDayExerciseByIdQuery(Guid Id) : IRequest<DayExerciseDetailsDto>;

public sealed record DayExerciseDetailsDto(Guid Id, Guid CourseDayId, Guid CourseAuthorId, Guid ExerciseId, int OrderNumber, int Sets, int Amount);

public sealed class GetDayExerciseByIdQueryHandler(IDayExerciseRepository dayExercises, ICourseDayRepository courseDays, IAppDbContext db) : IRequestHandler<GetDayExerciseByIdQuery, DayExerciseDetailsDto>
{
    public async Task<DayExerciseDetailsDto> Handle(GetDayExerciseByIdQuery request, CancellationToken cancellationToken)
    {
        var dayExercise = await dayExercises.GetByIdAsync(request.Id, cancellationToken);
        if (dayExercise is null)
        {
            throw new NotFoundException($"Day exercise {request.Id} does not exist.");
        }

        var day = await courseDays.GetByIdAsync(dayExercise.CourseDayId, cancellationToken);
        if (day is null)
        {
            throw new NotFoundException($"Day exercise {request.Id} does not exist.");
        }

        var authorId = await db.Courses.Where(c => c.Id == day.CourseId).Select(c => c.AuthorId).SingleAsync(cancellationToken);

        return new DayExerciseDetailsDto(dayExercise.Id, dayExercise.CourseDayId, authorId, dayExercise.ExerciseId, dayExercise.OrderNumber, dayExercise.Sets, dayExercise.Amount);
    }
}
