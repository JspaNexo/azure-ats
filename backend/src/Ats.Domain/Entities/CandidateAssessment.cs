using Ats.Domain.Common;
using Ats.Domain.Events;
using Ats.Domain.ValueObjects;

namespace Ats.Domain.Entities;

public sealed class CandidateAssessment : AggregateRoot<Guid>
{
    public Guid CandidateId { get; private set; }
    public string AssessmentType { get; private set; } = string.Empty;
    public AssessmentScores Scores { get; private set; } = default!;
    public string? RawResultsJson { get; private set; }
    public DateTime CompletedAtUtc { get; private set; }

    private CandidateAssessment() { }

    public static Result<CandidateAssessment> Create(Guid candidateId, AssessmentScores scores, string? rawResultsJson = null)
    {
        if (candidateId == Guid.Empty)
        {
            return Result.Failure<CandidateAssessment>(
                Error.Validation("CandidateAssessment.InvalidCandidateId", "El ID del candidato es obligatorio."));
        }

        if (scores == null)
        {
            return Result.Failure<CandidateAssessment>(
                Error.Validation("CandidateAssessment.ScoresRequired", "Las puntuaciones de la evaluacion son obligatorias."));
        }

        var assessment = new CandidateAssessment
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            AssessmentType = scores.AssessmentType,
            Scores = scores,
            RawResultsJson = rawResultsJson,
            CompletedAtUtc = DateTime.UtcNow
        };

        assessment.RaiseDomainEvent(new AssessmentCompletedDomainEvent(candidateId, assessment.Id, assessment.AssessmentType, DateTime.UtcNow));
        return Result.Success(assessment);
    }
}
