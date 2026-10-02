using Ats.Domain.Entities;

namespace Ats.Application.Common.Interfaces;

public record CandidateDetails(Candidate Candidate, CvAnalysis? CvAnalysis, DiscResult? DiscResult,
    DiscInterpretation? DiscInterpretation, InterviewReport? Report);

public interface ICandidateDetailsRepository
{
    Task<CandidateDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CandidateDetails>> GetAllAsync(string? recruiterId, CancellationToken cancellationToken = default);
}
