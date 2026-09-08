using Ats.Domain.Common;
using Ats.Domain.Enums;
using Ats.Domain.Events;
using Ats.Domain.ValueObjects;

namespace Ats.Domain.Entities;

public sealed class Candidate : AggregateRoot<Guid>
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public CandidateEmail Email { get; private set; } = default!;
    public string? PhoneNumber { get; private set; }
    public string EvaluatorDecision { get; private set; } = "Pending";
    public string? EvaluatorNotes { get; private set; }
    public DateTime? EvaluatedAtUtc { get; private set; }
    public string? AssignedRecruiterId { get; private set; }
    public string? AssignedRecruiterName { get; private set; }
    public string? AssignedRecruiterEmail { get; private set; }
    public DateTime? AssignedAtUtc { get; private set; }
    public string? TargetRole { get; private set; }
    public Guid? JobPositionId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private readonly List<CvDocument> _documents = [];
    public IReadOnlyCollection<CvDocument> Documents => _documents.AsReadOnly();

    private Candidate() { }

    private Candidate(Guid id, string firstName, string lastName, CandidateEmail email, string? phoneNumber)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        EvaluatorDecision = "Pending";
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static Result<Candidate> Create(
        string firstName,
        string lastName,
        CandidateEmail email,
        string? phoneNumber = null,
        Guid? jobPositionId = null,
        string? targetRole = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return Result.Failure<Candidate>(Error.Validation("Candidate.FirstNameEmpty", "El nombre es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return Result.Failure<Candidate>(Error.Validation("Candidate.LastNameEmpty", "El apellido es obligatorio."));
        }

        var candidate = new Candidate(Guid.NewGuid(), firstName.Trim(), lastName.Trim(), email, phoneNumber?.Trim())
        {
            JobPositionId = jobPositionId,
            TargetRole = targetRole?.Trim()
        };

        return Result.Success(candidate);
    }

    public void AssignToJobPosition(Guid? jobPositionId, string? targetRole)
    {
        JobPositionId = jobPositionId;
        if (!string.IsNullOrWhiteSpace(targetRole))
        {
            TargetRole = targetRole.Trim();
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetEvaluatorDecision(string decision, string? notes)
    {
        EvaluatorDecision = string.IsNullOrWhiteSpace(decision) ? "Pending" : decision.Trim();
        EvaluatorNotes = notes?.Trim();
        EvaluatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AssignToRecruiter(string? recruiterId, string? recruiterName, string? recruiterEmail)
    {
        AssignedRecruiterId = string.IsNullOrWhiteSpace(recruiterId) ? null : recruiterId.Trim();
        AssignedRecruiterName = string.IsNullOrWhiteSpace(recruiterName) ? null : recruiterName.Trim();
        AssignedRecruiterEmail = string.IsNullOrWhiteSpace(recruiterEmail) ? null : recruiterEmail.Trim();
        AssignedAtUtc = AssignedRecruiterId != null ? DateTime.UtcNow : null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public CvDocument AddCvDocument(string fileName, string storagePath, long fileSizeBytes, string contentType)
    {
        var document = CvDocument.Create(Id, fileName, storagePath, fileSizeBytes, contentType);
        _documents.Add(document);
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new CvUploadedDomainEvent(Id, document.Id, DateTime.UtcNow));
        return document;
    }
}

public sealed class CvDocument : Entity<Guid>
{
    public Guid CandidateId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public DateTime UploadedAtUtc { get; private set; }

    private CvDocument() { }

    internal static CvDocument Create(Guid candidateId, string fileName, string storagePath, long fileSizeBytes, string contentType)
    {
        return new CvDocument
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            FileName = fileName,
            StoragePath = storagePath,
            FileSizeBytes = fileSizeBytes,
            ContentType = contentType,
            UploadedAtUtc = DateTime.UtcNow
        };
    }
}

public sealed class CvAnalysis : AggregateRoot<Guid>
{
    public Guid CandidateId { get; private set; }
    public Guid DocumentId { get; private set; }
    public string AnalysisJson { get; private set; } = string.Empty;
    public ProcessingStatus Status { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public string ModelName { get; private set; } = string.Empty;
    public string PromptVersion { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private CvAnalysis() { }

    public static CvAnalysis CreatePending(Guid candidateId, Guid documentId, string providerName, string modelName, string promptVersion)
    {
        return new CvAnalysis
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            DocumentId = documentId,
            AnalysisJson = "{}",
            Status = ProcessingStatus.Pending,
            ProviderName = providerName,
            ModelName = modelName,
            PromptVersion = promptVersion,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkAsProcessed(string analysisJson)
    {
        AnalysisJson = analysisJson;
        Status = ProcessingStatus.Processed;
        ErrorMessage = null;
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new CvAnalysisCompletedDomainEvent(CandidateId, Id, DateTime.UtcNow));
    }

    public void UpdateDocument(Guid documentId)
    {
        DocumentId = documentId;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = ProcessingStatus.Failed;
        ErrorMessage = errorMessage;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public string? FeedbackJson { get; private set; }

    public void RecordEvaluatorFeedback(string feedbackJson)
    {
        FeedbackJson = feedbackJson;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class DiscResult : AggregateRoot<Guid>
{
    public Guid CandidateId { get; private set; }
    public DiscScores Scores { get; private set; } = default!;
    public DateTime CompletedAtUtc { get; private set; }

    private DiscResult() { }

    public static Result<DiscResult> Create(Guid candidateId, DiscScores scores)
    {
        var result = new DiscResult
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            Scores = scores,
            CompletedAtUtc = DateTime.UtcNow
        };

        result.RaiseDomainEvent(new DiscCompletedDomainEvent(candidateId, result.Id, DateTime.UtcNow));
        return Result.Success(result);
    }
}

public sealed class DiscInterpretation : AggregateRoot<Guid>
{
    public Guid CandidateId { get; private set; }
    public Guid DiscResultId { get; private set; }
    public string InterpretationJson { get; private set; } = string.Empty;
    public ProcessingStatus Status { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public string ModelName { get; private set; } = string.Empty;
    public string PromptVersion { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private DiscInterpretation() { }

    public static DiscInterpretation CreatePending(Guid candidateId, Guid discResultId, string providerName, string modelName, string promptVersion)
    {
        return new DiscInterpretation
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            DiscResultId = discResultId,
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
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = ProcessingStatus.Failed;
        ErrorMessage = errorMessage;
    }
}

public sealed class InterviewReport : AggregateRoot<Guid>
{
    public Guid CandidateId { get; private set; }
    public Guid? CvAnalysisId { get; private set; }
    public Guid? DiscInterpretationId { get; private set; }
    public string ReportContentJson { get; private set; } = string.Empty;
    public string? FileUrl { get; private set; }
    public ReportStatus Status { get; private set; }
    public int Version { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public string ModelName { get; private set; } = string.Empty;
    public string PromptVersion { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }
    public DateTime? GeneratedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private InterviewReport() { }

    public static InterviewReport CreateInitial(Guid candidateId, int version = 1)
    {
        return new InterviewReport
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            ReportContentJson = "{}",
            Status = ReportStatus.WaitingForCv,
            Version = version,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void UpdateStatus(ReportStatus newStatus)
    {
        Status = newStatus;
    }

    public void MarkAsGenerated(
        Guid cvAnalysisId,
        Guid discInterpretationId,
        string reportContentJson,
        string? fileUrl,
        string providerName,
        string modelName,
        string promptVersion)
    {
        CvAnalysisId = cvAnalysisId;
        DiscInterpretationId = discInterpretationId;
        ReportContentJson = reportContentJson;
        FileUrl = fileUrl;
        Status = ReportStatus.Generated;
        ProviderName = providerName;
        ModelName = modelName;
        PromptVersion = promptVersion;
        ErrorMessage = null;
        GeneratedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new InterviewReportGeneratedDomainEvent(CandidateId, Id, Version, DateTime.UtcNow));
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = ReportStatus.Failed;
        ErrorMessage = errorMessage;
    }
}

public sealed class ProcessingJob : Entity<Guid>
{
    public Guid CandidateId { get; private set; }
    public ProcessType ProcessType { get; private set; }
    public ProcessingStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public Guid EventId { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? FinishedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private ProcessingJob() { }

    public static ProcessingJob Create(Guid candidateId, ProcessType processType, Guid eventId, Guid correlationId)
    {
        return new ProcessingJob
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            ProcessType = processType,
            Status = ProcessingStatus.Pending,
            Attempts = 0,
            EventId = eventId,
            CorrelationId = correlationId,
            StartedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void IncrementAttempts() => Attempts++;

    public void MarkAsProcessing()
    {
        Status = ProcessingStatus.Processing;
        IncrementAttempts();
    }

    public void MarkAsProcessed()
    {
        Status = ProcessingStatus.Processed;
        FinishedAtUtc = DateTime.UtcNow;
        ErrorMessage = null;
        ErrorCode = null;
    }

    public void MarkAsFailed(string errorCode, string errorMessage)
    {
        Status = ProcessingStatus.Failed;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        FinishedAtUtc = DateTime.UtcNow;
    }
}

public sealed class JobPosition : AggregateRoot<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public string Seniority { get; private set; } = "Senior";
    public int MinExperienceYears { get; private set; } = 3;
    public string? Description { get; private set; }
    public string? Requirements { get; private set; }
    public string Status { get; private set; } = "Active"; // Active, Paused, Closed
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private JobPosition() { }

    private JobPosition(Guid id, string title, string department, string seniority, int minExperienceYears, string? description, string? requirements)
        : base(id)
    {
        Title = title;
        Department = department;
        Seniority = seniority;
        MinExperienceYears = minExperienceYears;
        Description = description;
        Requirements = requirements;
        Status = "Active";
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static Result<JobPosition> Create(
        string title,
        string department,
        string seniority = "Senior",
        int minExperienceYears = 3,
        string? description = null,
        string? requirements = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<JobPosition>(Error.Validation("JobPosition.TitleEmpty", "El título de la vacante es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(department))
        {
            return Result.Failure<JobPosition>(Error.Validation("JobPosition.DepartmentEmpty", "El departamento de la vacante es obligatorio."));
        }

        return Result.Success(new JobPosition(
            Guid.NewGuid(),
            title.Trim(),
            department.Trim(),
            string.IsNullOrWhiteSpace(seniority) ? "Senior" : seniority.Trim(),
            Math.Max(0, minExperienceYears),
            description?.Trim(),
            requirements?.Trim()));
    }

    public void UpdateStatus(string status)
    {
        Status = status switch
        {
            "Paused" => "Paused",
            "Closed" => "Closed",
            _ => "Active"
        };
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(string title, string department, string seniority, int minExperienceYears, string? description, string? requirements)
    {
        if (!string.IsNullOrWhiteSpace(title)) Title = title.Trim();
        if (!string.IsNullOrWhiteSpace(department)) Department = department.Trim();
        if (!string.IsNullOrWhiteSpace(seniority)) Seniority = seniority.Trim();
        MinExperienceYears = Math.Max(0, minExperienceYears);
        Description = description?.Trim();
        Requirements = requirements?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

