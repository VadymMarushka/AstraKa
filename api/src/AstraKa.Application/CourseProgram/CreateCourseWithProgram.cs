using AstraKa.Application.Common;
using FluentValidation;
using MediatR;

namespace AstraKa.Application.CourseProgram;

/// <summary>
/// Doubles as the wire DTO, same reasoning as <see cref="Courses.CreateCourseCommand"/>. Creates
/// the course (always a Draft) and its full base program in one ADO.NET transaction via
/// <see cref="ICourseProgramRepository"/>, which already owns commit/rollback - this is the lab 2
/// "business operation in a transaction" shown through the API. No pre-check of ExerciseIds: a bad
/// one rolls everything back, which is the point of this endpoint, not a failure to avoid.
/// </summary>
public sealed record CreateCourseWithProgramCommand(
    string Name,
    string Description,
    string Promise,
    decimal Price,
    IReadOnlyList<CreateCourseWithProgramDayInput> Days) : IRequest<Guid>;

public sealed record CreateCourseWithProgramDayInput(
    int DayNumber,
    string Title,
    string? Description,
    string? MeetLink,
    IReadOnlyList<CreateCourseWithProgramExerciseInput> Exercises);

public sealed record CreateCourseWithProgramExerciseInput(Guid ExerciseId, int OrderNumber, int Sets, int Amount);

public sealed class CreateCourseWithProgramCommandValidator : AbstractValidator<CreateCourseWithProgramCommand>
{
    public CreateCourseWithProgramCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Promise).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);

        RuleForEach(x => x.Days).ChildRules(day =>
        {
            day.RuleFor(d => d.DayNumber).GreaterThan(0);
            day.RuleFor(d => d.Title).NotEmpty();
            day.RuleForEach(d => d.Exercises).ChildRules(exercise =>
            {
                exercise.RuleFor(e => e.OrderNumber).GreaterThan(0);
                exercise.RuleFor(e => e.Sets).GreaterThan(0);
                exercise.RuleFor(e => e.Amount).GreaterThan(0);
            });
        });
    }
}

public sealed class CreateCourseWithProgramCommandHandler(ICourseProgramRepository courseProgram, ICurrentUser currentUser) : IRequestHandler<CreateCourseWithProgramCommand, Guid>
{
    public Task<Guid> Handle(CreateCourseWithProgramCommand request, CancellationToken cancellationToken)
    {
        var input = new CourseProgramInput(
            currentUser.UserId!.Value,
            request.Name,
            request.Description,
            request.Promise,
            request.Price,
            request.Days
                .Select(d => new CourseProgramDayInput(
                    d.DayNumber,
                    d.Title,
                    d.Description,
                    d.MeetLink,
                    d.Exercises.Select(e => new CourseProgramExerciseInput(e.ExerciseId, e.OrderNumber, e.Sets, e.Amount)).ToList()))
                .ToList());

        return courseProgram.CreateCourseWithProgramAsync(input, cancellationToken);
    }
}
