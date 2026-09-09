using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ats.Domain.Entities;

namespace Ats.Infrastructure.Persistence.Configurations;

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ToTable("candidates");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();

        builder.OwnsOne(c => c.Email, emailBuilder =>
        {
            emailBuilder.Property(e => e.Value)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();
            emailBuilder.HasIndex(e => e.Value).IsUnique();
        });

        builder.Property(c => c.PhoneNumber).HasMaxLength(30);
        builder.Property(c => c.EvaluatorDecision).HasMaxLength(50).HasDefaultValue("Pending");
        builder.Property(c => c.EvaluatorNotes);
        builder.Property(c => c.EvaluatedAtUtc);
        builder.Property(c => c.AssignedRecruiterId).HasColumnName("assignedrecruiterid").HasMaxLength(100);
        builder.Property(c => c.AssignedRecruiterName).HasColumnName("assignedrecruitername").HasMaxLength(150);
        builder.Property(c => c.AssignedRecruiterEmail).HasColumnName("assignedrecruiteremail").HasMaxLength(150);
        builder.Property(c => c.AssignedAtUtc).HasColumnName("assignedatutc");
        builder.Property(c => c.TargetRole).HasColumnName("target_role").HasMaxLength(150);
        builder.Property(c => c.JobPositionId).HasColumnName("job_position_id");
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.HasOne<JobPosition>()
            .WithMany()
            .HasForeignKey(c => c.JobPositionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.Documents)
            .WithOne()
            .HasForeignKey(d => d.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Documents).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class JobPositionConfiguration : IEntityTypeConfiguration<JobPosition>
{
    public void Configure(EntityTypeBuilder<JobPosition> builder)
    {
        builder.ToTable("job_positions");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Id).HasColumnName("id");
        builder.Property(j => j.Title).HasColumnName("title").HasMaxLength(150).IsRequired();
        builder.Property(j => j.Department).HasColumnName("department").HasMaxLength(100).IsRequired();
        builder.Property(j => j.Seniority).HasColumnName("seniority").HasMaxLength(50).HasDefaultValue("Senior");
        builder.Property(j => j.MinExperienceYears).HasColumnName("min_experience_years").HasDefaultValue(3);
        builder.Property(j => j.Description).HasColumnName("description");
        builder.Property(j => j.Requirements).HasColumnName("requirements");
        builder.Property(j => j.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("Active");
        builder.Property(j => j.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(j => j.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(j => j.Status);
    }
}

public class CvDocumentConfiguration : IEntityTypeConfiguration<CvDocument>
{
    public void Configure(EntityTypeBuilder<CvDocument> builder)
    {
        builder.ToTable("cv_documents");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.FileSizeBytes).IsRequired();
        builder.Property(d => d.UploadedAtUtc).IsRequired();

        builder.HasIndex(d => d.CandidateId);
    }
}

public class CvAnalysisConfiguration : IEntityTypeConfiguration<CvAnalysis>
{
    public void Configure(EntityTypeBuilder<CvAnalysis> builder)
    {
        builder.ToTable("candidate_cv_analyses");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AnalysisJson).HasColumnType("jsonb").IsRequired();
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.ProviderName).HasMaxLength(50).IsRequired();
        builder.Property(a => a.ModelName).HasMaxLength(50).IsRequired();
        builder.Property(a => a.PromptVersion).HasMaxLength(20).IsRequired();

        builder.HasIndex(a => a.CandidateId);

        builder.HasOne<Candidate>()
            .WithMany()
            .HasForeignKey(a => a.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CvDocument>()
            .WithMany()
            .HasForeignKey(a => a.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DiscResultConfiguration : IEntityTypeConfiguration<DiscResult>
{
    public void Configure(EntityTypeBuilder<DiscResult> builder)
    {
        builder.ToTable("candidate_disc_results");
        builder.HasKey(d => d.Id);

        builder.OwnsOne(d => d.Scores, scoresBuilder =>
        {
            scoresBuilder.Property(s => s.Dominance).HasColumnName("dominance").IsRequired();
            scoresBuilder.Property(s => s.Influence).HasColumnName("influence").IsRequired();
            scoresBuilder.Property(s => s.Steadiness).HasColumnName("steadiness").IsRequired();
            scoresBuilder.Property(s => s.Conscientiousness).HasColumnName("conscientiousness").IsRequired();
            scoresBuilder.Property(s => s.PrimaryStyle).HasColumnName("primary_style").HasMaxLength(10).IsRequired();
        });

        builder.HasIndex(d => d.CandidateId);

        builder.HasOne<Candidate>()
            .WithMany()
            .HasForeignKey(d => d.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DiscInterpretationConfiguration : IEntityTypeConfiguration<DiscInterpretation>
{
    public void Configure(EntityTypeBuilder<DiscInterpretation> builder)
    {
        builder.ToTable("candidate_disc_interpretations");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InterpretationJson).HasColumnType("jsonb").IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(i => i.ProviderName).HasMaxLength(50).IsRequired();
        builder.Property(i => i.ModelName).HasMaxLength(50).IsRequired();
        builder.Property(i => i.PromptVersion).HasMaxLength(20).IsRequired();

        builder.HasIndex(i => i.CandidateId);

        builder.HasOne<Candidate>()
            .WithMany()
            .HasForeignKey(i => i.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<DiscResult>()
            .WithMany()
            .HasForeignKey(i => i.DiscResultId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CandidateAssessmentConfiguration : IEntityTypeConfiguration<CandidateAssessment>
{
    public void Configure(EntityTypeBuilder<CandidateAssessment> builder)
    {
        builder.ToTable("candidate_assessments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AssessmentType).HasColumnName("assessment_type").HasMaxLength(50).IsRequired();
        builder.Property(a => a.RawResultsJson).HasColumnName("raw_results_json").HasColumnType("jsonb");
        builder.Property(a => a.CompletedAtUtc).HasColumnName("completed_at_utc").IsRequired();

        builder.OwnsOne(a => a.Scores, scoresBuilder =>
        {
            scoresBuilder.Property(s => s.AssessmentType).HasColumnName("scores_assessment_type").HasMaxLength(50);
            scoresBuilder.Property(s => s.PrimaryStyle).HasColumnName("primary_style").HasMaxLength(50).IsRequired();
            scoresBuilder.Property(s => s.Dimensions)
                .HasColumnName("dimensions_json")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, double>())
                .IsRequired();
        });

        builder.HasIndex(a => a.CandidateId);

        builder.HasOne<Candidate>()
            .WithMany()
            .HasForeignKey(a => a.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AssessmentInterpretationConfiguration : IEntityTypeConfiguration<AssessmentInterpretation>
{
    public void Configure(EntityTypeBuilder<AssessmentInterpretation> builder)
    {
        builder.ToTable("candidate_assessment_interpretations");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.AssessmentType).HasColumnName("assessment_type").HasMaxLength(50).IsRequired();
        builder.Property(i => i.InterpretationJson).HasColumnName("interpretation_json").HasColumnType("jsonb").IsRequired();
        builder.Property(i => i.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(i => i.ProviderName).HasColumnName("provider_name").HasMaxLength(50).IsRequired();
        builder.Property(i => i.ModelName).HasColumnName("model_name").HasMaxLength(50).IsRequired();
        builder.Property(i => i.PromptVersion).HasColumnName("prompt_version").HasMaxLength(20).IsRequired();
        builder.Property(i => i.ErrorMessage).HasColumnName("error_message");
        builder.Property(i => i.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(i => i.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(i => i.CandidateId);

        builder.HasOne<Candidate>()
            .WithMany()
            .HasForeignKey(i => i.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CandidateAssessment>()
            .WithMany()
            .HasForeignKey(i => i.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InterviewReportConfiguration : IEntityTypeConfiguration<InterviewReport>
{
    public void Configure(EntityTypeBuilder<InterviewReport> builder)
    {
        builder.ToTable("candidate_interview_reports");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReportContentJson).HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.FileUrl).HasMaxLength(1000);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(r => r.ProviderName).HasMaxLength(50).IsRequired();
        builder.Property(r => r.ModelName).HasMaxLength(50).IsRequired();
        builder.Property(r => r.PromptVersion).HasMaxLength(20).IsRequired();

        builder.HasIndex(r => r.CandidateId);

        builder.HasOne<Candidate>()
            .WithMany()
            .HasForeignKey(r => r.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CvAnalysis>()
            .WithMany()
            .HasForeignKey(r => r.CvAnalysisId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<DiscInterpretation>()
            .WithMany()
            .HasForeignKey(r => r.DiscInterpretationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ProcessingJobConfiguration : IEntityTypeConfiguration<ProcessingJob>
{
    public void Configure(EntityTypeBuilder<ProcessingJob> builder)
    {
        builder.ToTable("processing_jobs");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.ProcessType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(j => j.ErrorCode).HasMaxLength(50);
        builder.Property(j => j.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(j => j.EventId).IsUnique();
        builder.HasIndex(j => j.CorrelationId);
        builder.HasIndex(j => j.CandidateId);

        builder.HasOne<Candidate>()
            .WithMany()
            .HasForeignKey(j => j.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

