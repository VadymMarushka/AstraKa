using AstraKa.Application.Common;
using AstraKa.Domain.Entities;
using AstraKa.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Exercises;

/// <summary>
/// Filterable, sortable, paginated catalog listing - any authenticated caller. Projects straight
/// to <see cref="ExerciseDto"/> with <c>AsNoTracking</c>, never materializing full entities.
/// </summary>
public sealed record GetExercisesQuery(
    ExerciseUnit? Unit = null,
    string? NameSearch = null,
    bool? SystemOnly = null,
    Guid? AuthorId = null,
    ExerciseSortBy SortBy = ExerciseSortBy.Name,
    bool Descending = false,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ExerciseDto>>;

public enum ExerciseSortBy
{
    Name,
    Unit,
}

public sealed record ExerciseDto(Guid Id, Guid? AuthorId, string Name, string Description, string Instructions, ExerciseUnit Unit, string? VideoUrl, bool IsSystemExercise);

public sealed class GetExercisesQueryValidator : AbstractValidator<GetExercisesQuery>
{
    public GetExercisesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}

public sealed class GetExercisesQueryHandler(IAppDbContext db) : IRequestHandler<GetExercisesQuery, PagedResult<ExerciseDto>>
{
    public async Task<PagedResult<ExerciseDto>> Handle(GetExercisesQuery request, CancellationToken cancellationToken)
    {
        var filtered = BuildFilteredQuery(db.Exercises.AsNoTracking(), request);

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await BuildPagedQuery(filtered, request).ToListAsync(cancellationToken);

        return new PagedResult<ExerciseDto>(items, request.Page, request.PageSize, totalCount);
    }

    /// <summary>
    /// Filters + sorts, shared by the paged items query and the count query above - extracted so
    /// <c>QueryTranslationTests</c> can capture this handler's real SQL shape via
    /// <c>IQueryable.ToQueryString()</c> instead of re-implementing a simplified stand-in.
    /// </summary>
    internal static IQueryable<Exercise> BuildFilteredQuery(IQueryable<Exercise> exercises, GetExercisesQuery request)
    {
        var query = exercises;

        if (request.Unit is { } unit)
        {
            query = query.Where(e => e.Unit == unit);
        }

        if (!string.IsNullOrWhiteSpace(request.NameSearch))
        {
            query = query.Where(e => e.Name.Contains(request.NameSearch));
        }

        if (request.SystemOnly == true)
        {
            query = query.Where(e => e.AuthorId == null);
        }

        if (request.AuthorId is { } authorId)
        {
            query = query.Where(e => e.AuthorId == authorId);
        }

        return request.SortBy switch
        {
            ExerciseSortBy.Unit => request.Descending
                ? query.OrderByDescending(e => e.Unit).ThenBy(e => e.Name)
                : query.OrderBy(e => e.Unit).ThenBy(e => e.Name),
            _ => request.Descending ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
        };
    }

    /// <summary>The paged, projected items query - built on top of <see cref="BuildFilteredQuery"/>.</summary>
    internal static IQueryable<ExerciseDto> BuildPagedQuery(IQueryable<Exercise> filtered, GetExercisesQuery request) =>
        filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new ExerciseDto(e.Id, e.AuthorId, e.Name, e.Description, e.Instructions, e.Unit, e.VideoUrl, e.AuthorId == null));
}
