using Ats.Application.Common.Interfaces;
using Ats.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ats.Infrastructure.Persistence.Repositories;

public sealed class CandidateDetailsRepository(ApplicationDbContext context) : ICandidateDetailsRepository
{
    public async Task<CandidateDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var candidate = await context.Candidates.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (candidate is null) return null;
        return (await LoadDetailsAsync([candidate], cancellationToken)).Single();
    }

    public async Task<IReadOnlyList<CandidateDetails>> GetAllAsync(string? recruiterId, CancellationToken cancellationToken = default)
    {
        var query = context.Candidates.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(recruiterId)) query = query.Where(c => c.AssignedRecruiterId == recruiterId);
        var candidates = await query.OrderByDescending(c => c.CreatedAtUtc).ToListAsync(cancellationToken);
        return await LoadDetailsAsync(candidates, cancellationToken);
    }

    private async Task<IReadOnlyList<CandidateDetails>> LoadDetailsAsync(IReadOnlyList<Candidate> candidates,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0) return [];
        var ids = candidates.Select(c => c.Id).ToArray();

        // Batch each relation instead of executing four queries for every candidate.
        var analyses = await context.CvAnalyses.AsNoTracking().Where(a => ids.Contains(a.CandidateId))
            .OrderByDescending(a => a.CreatedAtUtc).ToListAsync(cancellationToken);
        var results = await context.DiscResults.AsNoTracking().Where(r => ids.Contains(r.CandidateId))
            .OrderByDescending(r => r.CompletedAtUtc).ToListAsync(cancellationToken);
        var interpretations = await context.DiscInterpretations.AsNoTracking().Where(i => ids.Contains(i.CandidateId))
            .OrderByDescending(i => i.CreatedAtUtc).ToListAsync(cancellationToken);
        var reports = await context.InterviewReports.AsNoTracking().Where(r => ids.Contains(r.CandidateId))
            .OrderByDescending(r => r.CreatedAtUtc).ToListAsync(cancellationToken);

        var analysisByCandidate = analyses.GroupBy(a => a.CandidateId).ToDictionary(g => g.Key, g => g.First());
        var resultByCandidate = results.GroupBy(r => r.CandidateId).ToDictionary(g => g.Key, g => g.First());
        var interpretationByCandidate = interpretations.GroupBy(i => i.CandidateId).ToDictionary(g => g.Key, g => g.First());
        var reportByCandidate = reports.GroupBy(r => r.CandidateId).ToDictionary(g => g.Key, g => g.First());
        return candidates.Select(c => new CandidateDetails(c, analysisByCandidate.GetValueOrDefault(c.Id),
            resultByCandidate.GetValueOrDefault(c.Id), interpretationByCandidate.GetValueOrDefault(c.Id),
            reportByCandidate.GetValueOrDefault(c.Id))).ToList();
    }
}
