using AstraKa.Application.Common;
using AstraKa.Domain.Exceptions;
using MediatR;

namespace AstraKa.Application.CourseProgram;

/// <summary>No author/Oracle check here - that's the Api's resource-based <c>AuthorOrOracle</c> policy.</summary>
public sealed record DeleteDayCommand(Guid Id) : IRequest;

public sealed class DeleteDayCommandHandler(ICourseDayRepository courseDays) : IRequestHandler<DeleteDayCommand>
{
    public async Task Handle(DeleteDayCommand request, CancellationToken cancellationToken)
    {
        var outcome = await courseDays.DeleteAsync(request.Id, cancellationToken);

        switch (outcome)
        {
            case DeleteOutcome.NotFound:
                throw new NotFoundException($"Course day {request.Id} does not exist.");
            case DeleteOutcome.HasDependencies:
                throw new DomainException("This day still has completed exercises and cannot be deleted.");
        }
    }
}
