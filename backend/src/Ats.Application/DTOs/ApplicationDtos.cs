using System.Text.Json.Serialization;

namespace Ats.Application.DTOs;

public record CandidateDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    DateTime CreatedAtUtc,
    string TargetRole = "",
    string Seniority = "",
    int ExperienceYears = 0,
    int MatchScore = 0,
    string PrimaryDiscStyle = "",
    string Status = "Registered",
    string EvaluatorDecision = "Pending",
    string? EvaluatorNotes = null,
    DateTime? EvaluatedAtUtc = null,
    string? AssignedRecruiterId = null,
    string? AssignedRecruiterName = null,
    string? AssignedRecruiterEmail = null,
    DateTime? AssignedAtUtc = null,
    Guid? JobPositionId = null,
    CvAnalysisDto? CvAnalysis = null,
    DiscInterpretationDto? DiscInterpretation = null,
    InterviewReportDto? Report = null,
    JobFitResult? JobFitDetail = null,
    string AssessmentType = "DISC",
    Dictionary<string, double>? AssessmentScores = null,
    AssessmentInterpretationDto? AssessmentInterpretation = null);

public record CvAnalysisDto(
    [property: JsonPropertyName("professionalSummary")] string ProfessionalSummary = "",
    [property: JsonPropertyName("currentRole")] string CurrentRole = "",
    [property: JsonPropertyName("estimatedSeniority")] string EstimatedSeniority = "Senior",
    [property: JsonPropertyName("totalExperienceYears")] double TotalExperienceYears = 0,
    [property: JsonPropertyName("skills")] List<SkillDto>? Skills = null,
    [property: JsonPropertyName("languages")] List<LanguageDto>? Languages = null,
    [property: JsonPropertyName("education")] List<EducationDto>? Education = null,
    [property: JsonPropertyName("certifications")] List<CertificationDto>? Certifications = null,
    [property: JsonPropertyName("workExperience")] List<WorkExperienceDto>? WorkExperience = null,
    [property: JsonPropertyName("pointsToValidate")] List<string>? PointsToValidate = null,
    [property: JsonPropertyName("warnings")] List<string>? Warnings = null);

public record SkillDto(
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("normalizedName")] string NormalizedName = "",
    [property: JsonPropertyName("category")] string Category = "General",
    [property: JsonPropertyName("experienceYears")] double ExperienceYears = 0,
    [property: JsonPropertyName("evidence")] string Evidence = "",
    [property: JsonPropertyName("confidence")] double Confidence = 0.9);

public record LanguageDto(
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("level")] string Level = "",
    [property: JsonPropertyName("evidence")] string Evidence = "");

public record EducationDto(
    [property: JsonPropertyName("degree")] string Degree = "",
    [property: JsonPropertyName("institution")] string Institution = "",
    [property: JsonPropertyName("graduationYear")] int? GraduationYear = null);

public record CertificationDto(
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("issuer")] string Issuer = "",
    [property: JsonPropertyName("year")] int? Year = null);

public record WorkExperienceDto(
    [property: JsonPropertyName("role")] string Role = "",
    [property: JsonPropertyName("company")] string Company = "",
    [property: JsonPropertyName("durationYears")] double DurationYears = 0,
    [property: JsonPropertyName("keyAchievements")] List<string>? KeyAchievements = null);

public record DiscInterpretationDto(
    [property: JsonPropertyName("primaryStyle")] string PrimaryStyle = "D/C",
    [property: JsonPropertyName("summary")] string Summary = "",
    [property: JsonPropertyName("strengthsToExplore")] List<string>? StrengthsToExplore = null,
    [property: JsonPropertyName("pointsToExplore")] List<string>? PointsToExplore = null,
    [property: JsonPropertyName("behavioralQuestionTopics")] List<string>? BehavioralQuestionTopics = null,
    [property: JsonPropertyName("disclaimer")] string Disclaimer = "");

public record AssessmentInterpretationDto(
    [property: JsonPropertyName("assessmentType")] string AssessmentType = "DISC",
    [property: JsonPropertyName("primaryStyle")] string PrimaryStyle = "D/C",
    [property: JsonPropertyName("summary")] string Summary = "",
    [property: JsonPropertyName("strengthsToExplore")] List<string>? StrengthsToExplore = null,
    [property: JsonPropertyName("pointsToExplore")] List<string>? PointsToExplore = null,
    [property: JsonPropertyName("behavioralQuestionTopics")] List<string>? BehavioralQuestionTopics = null,
    [property: JsonPropertyName("disclaimer")] string Disclaimer = "");

public record InterviewQuestionsDto(
    [property: JsonPropertyName("professionalQuestions")] List<string>? ProfessionalQuestions = null,
    [property: JsonPropertyName("technicalQuestions")] List<string>? TechnicalQuestions = null,
    [property: JsonPropertyName("behavioralQuestions")] List<string>? BehavioralQuestions = null);

public record InterviewReportDto(
    [property: JsonPropertyName("candidateOverview")] CandidateOverviewDto CandidateOverview,
    [property: JsonPropertyName("professionalProfile")] ProfessionalProfileDto ProfessionalProfile,
    [property: JsonPropertyName("discSummary")] DiscSummaryDto DiscSummary,
    [property: JsonPropertyName("validationPoints")] List<ValidationPointDto> ValidationPoints,
    [property: JsonPropertyName("interviewGuide")] InterviewGuideDto InterviewGuide,
    [property: JsonPropertyName("disclaimer")] string Disclaimer,
    [property: JsonPropertyName("fileUrl")] string? FileUrl = null);

public record CandidateOverviewDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("currentRole")] string CurrentRole,
    [property: JsonPropertyName("experienceYears")] double ExperienceYears,
    [property: JsonPropertyName("professionalSummary")] string ProfessionalSummary);

public record ProfessionalProfileDto(
    [property: JsonPropertyName("mainSkills")] List<string> MainSkills,
    [property: JsonPropertyName("relevantExperience")] List<string> RelevantExperience,
    [property: JsonPropertyName("education")] List<string> Education,
    [property: JsonPropertyName("languages")] List<string> Languages,
    [property: JsonPropertyName("certifications")] List<string> Certifications);

public record DiscSummaryDto(
    [property: JsonPropertyName("primaryStyle")] string PrimaryStyle,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("strengthsToExplore")] List<string> StrengthsToExplore,
    [property: JsonPropertyName("pointsToExplore")] List<string> PointsToExplore,
    [property: JsonPropertyName("evaluationType")] string EvaluationType = "DISC");

public record ValidationPointDto(
    [property: JsonPropertyName("topic")] string Topic,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("source")] string Source);

public record InterviewGuideDto(
    [property: JsonPropertyName("professionalQuestions")] List<string> ProfessionalQuestions,
    [property: JsonPropertyName("technicalQuestions")] List<string> TechnicalQuestions,
    [property: JsonPropertyName("behavioralQuestions")] List<string> BehavioralQuestions);

public record RegisterCandidateRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber);

public record SubmitDiscResultRequest(
    Guid CandidateId,
    int Dominance,
    int Influence,
    int Steadiness,
    int Conscientiousness,
    string? PrimaryStyle);

public record UploadCvResponse(
    Guid DocumentId,
    Guid CandidateId,
    string FileName,
    long FileSizeBytes,
    string Status);

public record UpdateEvaluatorDecisionRequest(
    string Decision,
    string? Notes);

public record AssignCandidateRequest(
    string RecruiterId,
    string RecruiterName,
    string RecruiterEmail);

public record RecruiterDto(
    string Id,
    string FullName,
    string Email,
    string Role = "Recruiter",
    int ActiveAssignmentsCount = 0);

public record SubmitAssessmentResultRequest(
    Guid CandidateId,
    string AssessmentType,
    Dictionary<string, double> Dimensions,
    string? PrimaryStyle,
    string? RawResultsJson = null);

public record AssessmentScoresDto(
    string AssessmentType,
    IReadOnlyDictionary<string, double> Dimensions,
    string PrimaryStyle);


