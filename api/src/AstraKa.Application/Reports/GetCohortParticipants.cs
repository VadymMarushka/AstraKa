using AstraKa.Domain.Enums;
using MediatR;

namespace AstraKa.Application.Reports;

/// <summary>Thin pass-through to <see cref="IReportRepository.GetCohortParticipantsAsync"/> - no visibility/authorization here, the controller resolves that via <c>GetCohortByIdQuery</c> first.</summary>
public sealed record GetCohortParticipantsQuery(Guid CohortId, EnrollmentStatus? Status, CohortParticipantSort SortBy, bool Descending) : IRequest<IReadOnlyList<CohortParticipantRecord>>;

public sealed class GetCohortParticipantsQueryHandler(IReportRepository reports) : IRequestHandler<GetCohortParticipantsQuery, IReadOnlyList<CohortParticipantRecord>>
{
    public Task<IReadOnlyList<CohortParticipantRecord>> Handle(GetCohortParticipantsQuery request, CancellationToken cancellationToken)
    {
        return reports.GetCohortParticipantsAsync(request.CohortId, request.Status, request.SortBy, request.Descending, cancellationToken);
    }
}
