using Microsoft.EntityFrameworkCore;
using Ats.Application.Common.Interfaces;
using Ats.Domain.Entities;

namespace Ats.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    public UnitOfWork(ApplicationDbContext context) => _context = context;
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => _context.SaveChangesAsync(cancellationToken);
}

public class CandidateRepository : ICandidateRepository
{
    private readonly ApplicationDbContext _context;
    public CandidateRepository(ApplicationDbContext context) => _context = context;
    public Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => _context.Candidates.Include(c => c.Documents).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    public Task<Candidate?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => _context.Candidates.AsNoTracking().Include(c => c.Documents).FirstOrDefaultAsync(c => c.Email.Value == email, cancellationToken);
    public async Task<IReadOnlyList<Candidate>> GetAllAsync(CancellationToken cancellationToken = default) => await _context.Candidates.AsNoTracking().Include(c => c.Documents).OrderByDescending(c => c.CreatedAtUtc).ToListAsync(cancellationToken);
    public async Task AddAsync(Candidate candidate, CancellationToken cancellationToken = default) => await _context.Candidates.AddAsync(candidate, cancellationToken);
    public async Task AddCvDocumentAsync(CvDocument document, CancellationToken cancellationToken = default) => await _context.CvDocuments.AddAsync(document, cancellationToken);
    public void Update(Candidate candidate)
    {
        var entry = _context.Entry(candidate);
        if (entry.State == EntityState.Detached) { _context.Candidates.Attach(candidate); entry.State = EntityState.Modified; }
    }
}

public class CvAnalysisRepository : ICvAnalysisRepository
{
    private readonly ApplicationDbContext _context;
    public CvAnalysisRepository(ApplicationDbContext context) => _context = context;
    public Task<CvAnalysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => _context.CvAnalyses.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    public Task<CvAnalysis?> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default) => _context.CvAnalyses.AsNoTracking().OrderByDescending(a => a.CreatedAtUtc).FirstOrDefaultAsync(a => a.CandidateId == candidateId, cancellationToken);
    
    public async Task<IReadOnlyList<CvAnalysis>> GetByCandidateIdsAsync(IEnumerable<Guid> candidateIds, CancellationToken cancellationToken = default)
    {
        var all = await _context.CvAnalyses.AsNoTracking().Where(a => candidateIds.Contains(a.CandidateId)).ToListAsync(cancellationToken);
        return all.GroupBy(a => a.CandidateId).Select(g => g.OrderByDescending(a => a.CreatedAtUtc).First()).ToList();
    }
    
    public async Task AddAsync(CvAnalysis cvAnalysis, CancellationToken cancellationToken = default) => await _context.CvAnalyses.AddAsync(cvAnalysis, cancellationToken);
    public void Update(CvAnalysis cvAnalysis)
    {
        var entry = _context.Entry(cvAnalysis);
        if (entry.State == EntityState.Detached) { _context.CvAnalyses.Attach(cvAnalysis); entry.State = EntityState.Modified; }
    }
}

public class DiscRepository : IDiscRepository
{
    private readonly ApplicationDbContext _context;
    public DiscRepository(ApplicationDbContext context) => _context = context;
    public Task<DiscResult?> GetResultByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default) => _context.DiscResults.AsNoTracking().OrderByDescending(d => d.CompletedAtUtc).FirstOrDefaultAsync(d => d.CandidateId == candidateId, cancellationToken);
    
    public async Task<IReadOnlyList<DiscResult>> GetResultsByCandidateIdsAsync(IEnumerable<Guid> candidateIds, CancellationToken cancellationToken = default)
    {
        var all = await _context.DiscResults.AsNoTracking().Where(d => candidateIds.Contains(d.CandidateId)).ToListAsync(cancellationToken);
        return all.GroupBy(d => d.CandidateId).Select(g => g.OrderByDescending(d => d.CompletedAtUtc).First()).ToList();
    }
    
    public Task<DiscInterpretation?> GetInterpretationByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default) => _context.DiscInterpretations.AsNoTracking().OrderByDescending(i => i.CreatedAtUtc).FirstOrDefaultAsync(i => i.CandidateId == candidateId, cancellationToken);
    
    public async Task<IReadOnlyList<DiscInterpretation>> GetInterpretationsByCandidateIdsAsync(IEnumerable<Guid> candidateIds, CancellationToken cancellationToken = default)
    {
        var all = await _context.DiscInterpretations.AsNoTracking().Where(i => candidateIds.Contains(i.CandidateId)).ToListAsync(cancellationToken);
        return all.GroupBy(i => i.CandidateId).Select(g => g.OrderByDescending(i => i.CreatedAtUtc).First()).ToList();
    }
    
    public async Task AddResultAsync(DiscResult discResult, CancellationToken cancellationToken = default) => await _context.DiscResults.AddAsync(discResult, cancellationToken);
    public async Task AddInterpretationAsync(DiscInterpretation discInterpretation, CancellationToken cancellationToken = default) => await _context.DiscInterpretations.AddAsync(discInterpretation, cancellationToken);
    public void UpdateInterpretation(DiscInterpretation discInterpretation)
    {
        var entry = _context.Entry(discInterpretation);
        if (entry.State == EntityState.Detached) { _context.DiscInterpretations.Attach(discInterpretation); entry.State = EntityState.Modified; }
    }
}

public class CandidateAssessmentRepository : ICandidateAssessmentRepository
{
    private readonly ApplicationDbContext _context;
    public CandidateAssessmentRepository(ApplicationDbContext context) => _context = context;

    public Task<CandidateAssessment?> GetResultByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default) =>
        _context.CandidateAssessments.AsNoTracking().OrderByDescending(a => a.CompletedAtUtc).FirstOrDefaultAsync(a => a.CandidateId == candidateId, cancellationToken);

    public async Task<IReadOnlyList<CandidateAssessment>> GetResultsByCandidateIdsAsync(IEnumerable<Guid> candidateIds, CancellationToken cancellationToken = default)
    {
        var all = await _context.CandidateAssessments.AsNoTracking().Where(a => candidateIds.Contains(a.CandidateId)).ToListAsync(cancellationToken);
        return all.GroupBy(a => a.CandidateId).Select(g => g.OrderByDescending(a => a.CompletedAtUtc).First()).ToList();
    }

    public Task<AssessmentInterpretation?> GetInterpretationByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default) =>
        _context.AssessmentInterpretations.AsNoTracking().OrderByDescending(i => i.CreatedAtUtc).FirstOrDefaultAsync(i => i.CandidateId == candidateId, cancellationToken);

    public async Task<IReadOnlyList<AssessmentInterpretation>> GetInterpretationsByCandidateIdsAsync(IEnumerable<Guid> candidateIds, CancellationToken cancellationToken = default)
    {
        var all = await _context.AssessmentInterpretations.AsNoTracking().Where(i => candidateIds.Contains(i.CandidateId)).ToListAsync(cancellationToken);
        return all.GroupBy(i => i.CandidateId).Select(g => g.OrderByDescending(i => i.CreatedAtUtc).First()).ToList();
    }

    public async Task AddResultAsync(CandidateAssessment assessment, CancellationToken cancellationToken = default) =>
        await _context.CandidateAssessments.AddAsync(assessment, cancellationToken);

    public async Task AddInterpretationAsync(AssessmentInterpretation interpretation, CancellationToken cancellationToken = default) =>
        await _context.AssessmentInterpretations.AddAsync(interpretation, cancellationToken);

    public void UpdateInterpretation(AssessmentInterpretation interpretation)
    {
        var entry = _context.Entry(interpretation);
        if (entry.State == EntityState.Detached)
        {
            _context.AssessmentInterpretations.Attach(interpretation);
            entry.State = EntityState.Modified;
        }
    }
}

public class InterviewReportRepository : IInterviewReportRepository
{
    private readonly ApplicationDbContext _context;
    public InterviewReportRepository(ApplicationDbContext context) => _context = context;
    public Task<InterviewReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => _context.InterviewReports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    public Task<InterviewReport?> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default) => _context.InterviewReports.AsNoTracking().OrderByDescending(r => r.CreatedAtUtc).FirstOrDefaultAsync(r => r.CandidateId == candidateId, cancellationToken);
    
    public async Task<IReadOnlyList<InterviewReport>> GetByCandidateIdsAsync(IEnumerable<Guid> candidateIds, CancellationToken cancellationToken = default)
    {
        var all = await _context.InterviewReports.AsNoTracking().Where(r => candidateIds.Contains(r.CandidateId)).ToListAsync(cancellationToken);
        return all.GroupBy(r => r.CandidateId).Select(g => g.OrderByDescending(r => r.CreatedAtUtc).First()).ToList();
    }
    
    public Task<InterviewReport?> GetLatestByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default) => _context.InterviewReports.AsNoTracking().OrderByDescending(r => r.Version).FirstOrDefaultAsync(r => r.CandidateId == candidateId, cancellationToken);
    public async Task AddAsync(InterviewReport report, CancellationToken cancellationToken = default) => await _context.InterviewReports.AddAsync(report, cancellationToken);
    public void Update(InterviewReport report)
    {
        var entry = _context.Entry(report);
        if (entry.State == EntityState.Detached) { _context.InterviewReports.Attach(report); entry.State = EntityState.Modified; }
    }
}

public class ProcessingJobRepository : IProcessingJobRepository
{
    private readonly ApplicationDbContext _context;
    public ProcessingJobRepository(ApplicationDbContext context) => _context = context;
    public Task<ProcessingJob?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default) => _context.ProcessingJobs.FirstOrDefaultAsync(j => j.EventId == eventId, cancellationToken);
    public Task<ProcessingJob?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default) => _context.ProcessingJobs.FirstOrDefaultAsync(j => j.CorrelationId == correlationId, cancellationToken);
    public async Task AddAsync(ProcessingJob job, CancellationToken cancellationToken = default) => await _context.ProcessingJobs.AddAsync(job, cancellationToken);
    public void Update(ProcessingJob job)
    {
        var entry = _context.Entry(job);
        if (entry.State == EntityState.Detached) { _context.ProcessingJobs.Attach(job); entry.State = EntityState.Modified; }
    }
}

public class JobPositionRepository : IJobPositionRepository
{
    private readonly ApplicationDbContext _context;
    public JobPositionRepository(ApplicationDbContext context) => _context = context;
    public Task<JobPosition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => _context.JobPositions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    
    public async Task<IReadOnlyList<JobPosition>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) => await _context.JobPositions.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
    
    public async Task<IReadOnlyList<JobPosition>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.JobPositions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(p => p.Status == status);
        return await query.OrderByDescending(p => p.CreatedAtUtc).ToListAsync(cancellationToken);
    }
    public async Task AddAsync(JobPosition position, CancellationToken cancellationToken = default) => await _context.JobPositions.AddAsync(position, cancellationToken);
    public void Update(JobPosition position)
    {
        var entry = _context.Entry(position);
        if (entry.State == EntityState.Detached) { _context.JobPositions.Attach(position); entry.State = EntityState.Modified; }
    }
}
