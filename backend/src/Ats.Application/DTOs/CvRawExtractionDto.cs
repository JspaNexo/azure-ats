using System.Text.Json.Serialization;

namespace Ats.Application.DTOs;

public record RawWorkEntryDto(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("company")] string Company,
    [property: JsonPropertyName("startDate")] string StartDate,
    [property: JsonPropertyName("endDate")] string EndDate,
    [property: JsonPropertyName("durationYears")] double DurationYears,
    [property: JsonPropertyName("responsibilities")] List<string> Responsibilities);

public record RawSkillMentionDto(
    [property: JsonPropertyName("rawText")] string RawText,
    [property: JsonPropertyName("contextSentence")] string ContextSentence,
    [property: JsonPropertyName("section")] string Section);

public record CvRawExtractionDto(
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("workEntries")] List<RawWorkEntryDto> WorkEntries,
    [property: JsonPropertyName("skillMentions")] List<RawSkillMentionDto> SkillMentions,
    [property: JsonPropertyName("education")] List<EducationDto> Education,
    [property: JsonPropertyName("certifications")] List<CertificationDto> Certifications,
    [property: JsonPropertyName("languages")] List<LanguageDto> Languages);
