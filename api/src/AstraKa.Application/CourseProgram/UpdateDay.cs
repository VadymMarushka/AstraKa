using AstraKa.Application.Common;
using FluentValidation;
using MediatR;

namespace AstraKa.Application.CourseProgram;

/// <summary>The id comes from the route. No author/Oracle check here - that's the Api's resource-based <c>AuthorOrOracle</c> policy.</summary>
public sealed record UpdateDayCommand(Guid Id, string Title, string? Description, string? MeetLink) : IRequest;

public sealed class UpdateDayCommandValidator : AbstractValidator<UpdateDayCommand>
{
    public UpdateDayCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
    }
}

public sealed class UpdateDayCommandHandler(ICourseDayRepository courseDays) : IRequestHandler<UpdateDayCommand>
{
    public async Task Handle(UpdateDayCommand request, CancellationToken cancellationToken)
    {
        var updated = await courseDays.UpdateAsync(request.Id, request.Title, request.Description, request.MeetLink, cancellationToken);
        if (!updated)
        {
            throw new NotFoundException($"Course day {request.Id} does not exist.");
        }
    }
}
