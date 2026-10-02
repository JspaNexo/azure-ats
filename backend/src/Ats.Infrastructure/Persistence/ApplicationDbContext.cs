using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
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

        // Match the SQL initialization scripts, retaining their legacy evaluator columns.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (entityType.IsOwned() && property.IsPrimaryKey())
                {
                    property.SetColumnName("id");
                    continue;
                }
                if (entityType.ClrType == typeof(Candidate) &&
                    property.Name is nameof(Candidate.EvaluatorDecision) or nameof(Candidate.EvaluatorNotes) or nameof(Candidate.EvaluatedAtUtc))
                    continue;

                var columnName = property.GetColumnName();
                property.SetColumnName(Regex.Replace(columnName, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
            }
        }
    }
}

