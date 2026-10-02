using Ats.Domain.Entities;
using Ats.Domain.Enums;

namespace Ats.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICandidateRepository
{
    Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Candidate?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Candidate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Candidate candidate, CancellationToken cancellationToken = default);
    Task AddCvDocumentAsync(CvDocument document, CancellationToken cancellationToken = default);
    void Update(Candidate candidate);
}

public interface ICvAnalysisRepository
{
    Task<CvAnalysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CvAnalysis?> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default);
    Task AddAsync(CvAnalysis cvAnalysis, CancellationToken cancellationToken = default);
    void Update(CvAnalysis cvAnalysis);
}

public interface IDiscRepository
{
    Task<DiscResult?> GetResultByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DiscResult?> GetResultByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default);
    Task<DiscInterpretation?> GetInterpretationByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default);
    Task AddResultAsync(DiscResult discResult, CancellationToken cancellationToken = default);
    Task AddInterpretationAsync(DiscInterpretation discInterpretation, CancellationToken cancellationToken = default);
    void UpdateInterpretation(DiscInterpretation discInterpretation);
}

public interface IInterviewReportRepository
{
    Task<InterviewReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InterviewReport?> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default);
    Task<InterviewReport?> GetLatestByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default);
    Task AddAsync(InterviewReport report, CancellationToken cancellationToken = default);
    void Update(InterviewReport report);
}

public interface IProcessingJobRepository
{
    Task<ProcessingJob?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<ProcessingJob?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessingJob job, CancellationToken cancellationToken = default);
    void Update(ProcessingJob job);
}

public interface IDocumentStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream?> GetFileAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default);
}

public interface IJobPositionRepository
{
    Task<JobPosition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobPosition>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default);
    Task AddAsync(JobPosition position, CancellationToken cancellationToken = default);
    void Update(JobPosition position);
}

public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}

