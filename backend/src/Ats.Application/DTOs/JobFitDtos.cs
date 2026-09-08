using System.Text.Json.Serialization;

namespace Ats.Application.DTOs;

public record SkillMatchDetail(
    [property: JsonPropertyName("skillName")] string SkillName,
    [property: JsonPropertyName("isMatched")] bool IsMatched,
    [property: JsonPropertyName("candidateConfidence")] double CandidateConfidence,
    [property: JsonPropertyName("experienceYears")] double ExperienceYears,
    [property: JsonPropertyName("notes")] string? Notes = null);

public record JobFitResult(
    [property: JsonPropertyName("overallScore")] int OverallScore,
    [property: JsonPropertyName("matches")] List<SkillMatchDetail> Matches,
    [property: JsonPropertyName("missingRequired")] List<string> MissingRequired,
    [property: JsonPropertyName("bonusSkills")] List<string> BonusSkills,
    [property: JsonPropertyName("seniorityAssessment")] string SeniorityAssessment,
    [property: JsonPropertyName("fitCategory")] string FitCategory);
