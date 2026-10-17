using FluentValidation;
using MediatR;

namespace AstraKa.Application.CourseProgram;

/// <summary>
/// No author/Oracle check here - that's the Api's resource-based <c>AuthorOrOracle</c> policy. A
/// duplicate <c>DayNumber</c> is caught by the DB's own unique index on (CourseId, DayNumber),
/// surfacing as a raw SqlException that <c>GlobalExceptionHandler</c> maps to 409 - no pre-check
/// needed here.
/// </summary>
public sealed record CreateDayCommand(Guid CourseId, int DayNumber, string Title, string? Description, string? MeetLink) : IRequest<Guid>;

public sealed class CreateDayCommandValidator : AbstractValidator<CreateDayCommand>
{
    public CreateDayCommandValidator()
    {
        RuleFor(x => x.DayNumber).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty();
    }
}

public sealed class CreateDayCommandHandler(ICourseDayRepository courseDays) : IRequestHandler<CreateDayCommand, Guid>
{
    public Task<Guid> Handle(CreateDayCommand request, CancellationToken cancellationToken)
    {
        return courseDays.CreateAsync(request.CourseId, request.DayNumber, request.Title, request.Description, request.MeetLink, cancellationToken);
    }
}
