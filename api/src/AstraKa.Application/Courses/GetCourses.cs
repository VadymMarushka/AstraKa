using AstraKa.Application.Common;
using AstraKa.Domain.Entities;
using AstraKa.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Courses;

/// <summary>
/// Filterable, sortable, paginated course listing. Visibility: a course's own author (via
/// <see cref="Mine"/>) and Oracle see every status; everyone else sees only <c>Published</c>.
/// </summary>
public sealed record GetCoursesQuery(
    string? NameSearch = null,
    int? SignId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool Mine = false,
    CourseSortBy SortBy = CourseSortBy.Name,
    bool Descending = false,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<CourseListItemDto>>;

public enum CourseSortBy
{
    Name,
    Price,
}

public sealed record CourseListItemDto(Guid Id, string Name, string AuthorName, IReadOnlyList<string> OpenToSignNames, int DayCount, CourseStatus Status, decimal Price);

public sealed class GetCoursesQueryValidator : AbstractValidator<GetCoursesQuery>
{
    public GetCoursesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage("MinPrice must not exceed MaxPrice.");
    }
}

public sealed class GetCoursesQueryHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetCoursesQuery, PagedResult<CourseListItemDto>>
{
    public async Task<PagedResult<CourseListItemDto>> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
    {
        var filtered = BuildFilteredQuery(db.Courses.AsNoTracking(), request, currentUser.UserId, currentUser.Role == UserRole.Oracle);

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await BuildPagedQuery(filtered, request).ToListAsync(cancellationToken);

        return new PagedResult<CourseListItemDto>(items, request.Page, request.PageSize, totalCount);
    }

    /// <summary>
    /// Filters + sorts, shared by the paged items query and the count query above - extracted so
    /// <c>QueryTranslationTests</c> can capture this handler's real SQL shape via
    /// <c>IQueryable.ToQueryString()</c> instead of re-implementing a simplified stand-in.
    /// </summary>
    internal static IQueryable<Course> BuildFilteredQuery(IQueryable<Course> courses, GetCoursesQuery request, Guid? currentUserId, bool currentUserIsOracle)
    {
        var query = courses;

        if (request.Mine)
        {
            query = query.Where(c => c.AuthorId == currentUserId);
        }
        else if (!currentUserIsOracle)
        {
            query = query.Where(c => c.Status == CourseStatus.Published);
        }

        if (request.SignId is { } signId)
        {
            query = query.Where(c => !c.CourseSigns.Any() || c.CourseSigns.Any(cs => cs.ZodiacSignId == signId));
        }

        if (!string.IsNullOrWhiteSpace(request.NameSearch))
        {
            query = query.Where(c => c.Name.Contains(request.NameSearch));
        }

        if (request.MinPrice is { } minPrice)
        {
            query = query.Where(c => c.Price >= minPrice);
        }

        if (request.MaxPrice is { } maxPrice)
        {
            query = query.Where(c => c.Price <= maxPrice);
        }

        return request.SortBy switch
        {
            CourseSortBy.Price => request.Descending
                ? query.OrderByDescending(c => c.Price).ThenBy(c => c.Name)
                : query.OrderBy(c => c.Price).ThenBy(c => c.Name),
            _ => request.Descending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
        };
    }

    /// <summary>The paged, projected items query - built on top of <see cref="BuildFilteredQuery"/>.</summary>
    internal static IQueryable<CourseListItemDto> BuildPagedQuery(IQueryable<Course> filtered, GetCoursesQuery request) =>
        filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseListItemDto(
                c.Id,
                c.Name,
                c.Author.User.Name,
                c.CourseSigns.Select(cs => cs.ZodiacSign.Name).ToList(),
                c.CourseDays.Count,
                c.Status,
                c.Price));
}
