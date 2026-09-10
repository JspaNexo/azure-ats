using Microsoft.EntityFrameworkCore;
using Ats.Domain.Entities;

namespace Ats.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<CvDocument> CvDocuments => Set<CvDocument>();
    public DbSet<CvAnalysis> CvAnalyses => Set<CvAnalysis>();
    public DbSet<DiscResult> DiscResults => Set<DiscResult>();
    public DbSet<DiscInterpretation> DiscInterpretations => Set<DiscInterpretation>();
    public DbSet<CandidateAssessment> CandidateAssessments => Set<CandidateAssessment>();
    public DbSet<AssessmentInterpretation> AssessmentInterpretations => Set<AssessmentInterpretation>();
    public DbSet<InterviewReport> InterviewReports => Set<InterviewReport>();
    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();
    public DbSet<JobPosition> JobPositions => Set<JobPosition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Ats.Domain.Common.AggregateRoot<Guid> agg && agg.GetDomainEvents().Count > 0)
            .Select(e => (Ats.Domain.Common.AggregateRoot<Guid>)e.Entity)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        return result;
    }
}

