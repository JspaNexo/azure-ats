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
    public DbSet<InterviewReport> InterviewReports => Set<InterviewReport>();
    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();
    public DbSet<JobPosition> JobPositions => Set<JobPosition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}

