using Ats.Domain.Common;
using Ats.Domain.Enums;
using Ats.Domain.Events;

namespace Ats.Domain.Entities;

public sealed class AssessmentInterpretation : AggregateRoot<Guid>
{
    public Guid CandidateId { get; private set; }
    public Guid AssessmentId { get; private set; }
    public string AssessmentType { get; private set; } = string.Empty;
    public string InterpretationJson { get; private set; } = string.Empty;
    public ProcessingStatus Status { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public string ModelName { get; private set; } = string.Empty;
    public string PromptVersion { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private AssessmentInterpretation() { }

    public static AssessmentInterpretation CreatePending(
        Guid candidateId,
        Guid assessmentId,
        string assessmentType,
        string providerName,
        string modelName,
        string promptVersion)
    {
        return new AssessmentInterpretation
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            AssessmentId = assessmentId,
            AssessmentType = assessmentType,
            InterpretationJson = "{}",
            Status = ProcessingStatus.Pending,
            ProviderName = providerName,
            ModelName = modelName,
            PromptVersion = promptVersion,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkAsProcessed(string interpretationJson)
    {
        InterpretationJson = interpretationJson;
        Status = ProcessingStatus.Processed;
        ErrorMessage = null;
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new AssessmentInterpretationCompletedDomainEvent(CandidateId, Id, AssessmentType, DateTime.UtcNow));
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = ProcessingStatus.Failed;
        ErrorMessage = errorMessage;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
