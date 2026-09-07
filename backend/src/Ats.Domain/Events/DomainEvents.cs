using Ats.Domain.Common;

namespace Ats.Domain.Events;

public record CvUploadedDomainEvent(
    Guid CandidateId,
    Guid DocumentId,
    DateTime OccurredOnUtc) : IDomainEvent;

public record CvAnalysisCompletedDomainEvent(
    Guid CandidateId,
    Guid CvAnalysisId,
    DateTime OccurredOnUtc) : IDomainEvent;

public record DiscCompletedDomainEvent(
    Guid CandidateId,
    Guid DiscResultId,
    DateTime OccurredOnUtc) : IDomainEvent;

public record InterviewReportGeneratedDomainEvent(
    Guid CandidateId,
    Guid ReportId,
    int Version,
    DateTime OccurredOnUtc) : IDomainEvent;

