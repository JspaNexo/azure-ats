using System.Text.Json;
using Ats.Application.DTOs;
using Ats.Application.Common.Interfaces;
using Ats.Domain.Entities;
using Ats.Domain.Enums;

namespace Ats.Application.Features.Candidates;

internal static class CandidateDtoMapper
{
    public static CandidateDto Map(CandidateDetails details) => Map(details.Candidate, details.CvAnalysis,
        details.DiscResult, details.DiscInterpretation, details.Report);
    public static CandidateDto Map(Candidate candidate, CvAnalysis? analysis, DiscResult? discResult,
        DiscInterpretation? interpretation, InterviewReport? report)
    {
        var cv = analysis?.Status == ProcessingStatus.Processed ? Deserialize<CvAnalysisDto>(analysis.AnalysisJson) : null;
        var disc = interpretation?.Status == ProcessingStatus.Processed ? Deserialize<DiscInterpretationDto>(interpretation.InterpretationJson) : null;
        var reportDto = report?.Status == ReportStatus.Generated ? Deserialize<InterviewReportDto>(report.ReportContentJson) : null;

        return new CandidateDto(candidate.Id, candidate.FirstName, candidate.LastName, candidate.Email.Value,
            candidate.PhoneNumber, candidate.CreatedAtUtc,
            TargetRole: candidate.TargetRole ?? cv?.CurrentRole ?? "Sin vacante asignada",
            Seniority: cv?.EstimatedSeniority ?? "Sin evaluar",
            ExperienceYears: cv is null ? null : (int)Math.Round(cv.TotalExperienceYears),
            PrimaryDiscStyle: discResult?.Scores.PrimaryStyle ?? disc?.PrimaryStyle ?? "Sin evaluar",
            Status: reportDto is not null ? "ReportReady" : disc is not null ? "DiscEvaluated" : cv is not null ? "CvAnalyzed" : "Registered",
            EvaluatorDecision: candidate.EvaluatorDecision, EvaluatorNotes: candidate.EvaluatorNotes,
            EvaluatedAtUtc: candidate.EvaluatedAtUtc, AssignedRecruiterId: candidate.AssignedRecruiterId,
            AssignedRecruiterName: candidate.AssignedRecruiterName, AssignedRecruiterEmail: candidate.AssignedRecruiterEmail,
            AssignedAtUtc: candidate.AssignedAtUtc, JobPositionId: candidate.JobPositionId,
            CvAnalysis: cv, DiscInterpretation: disc, Report: reportDto,
            DiscScores: discResult is null ? null : new DiscScoresDto(discResult.Scores.Dominance,
                discResult.Scores.Influence, discResult.Scores.Steadiness, discResult.Scores.Conscientiousness));
    }

    private static T? Deserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}") return null;
        try { return JsonSerializer.Deserialize<T>(json); }
        catch (JsonException) { return null; }
    }
}
