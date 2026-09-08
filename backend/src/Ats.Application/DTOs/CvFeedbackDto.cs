using System.Text.Json.Serialization;

namespace Ats.Application.DTOs;

public record CvFeedbackDto(
    [property: JsonPropertyName("correctSkills")] List<string> CorrectSkills,
    [property: JsonPropertyName("incorrectSkills")] List<string> IncorrectSkills,
    [property: JsonPropertyName("missedSkills")] List<string> MissedSkills,
    [property: JsonPropertyName("actualSeniority")] string? ActualSeniority = null,
    [property: JsonPropertyName("notes")] string? Notes = null);
