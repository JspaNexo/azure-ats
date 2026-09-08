using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Domain.Entities;

namespace Ats.Application.Features.Scoring;

public class JobFitScoringService
{
    private readonly ISkillNormalizationService _skillNormalizer;

    public JobFitScoringService(ISkillNormalizationService skillNormalizer)
    {
        _skillNormalizer = skillNormalizer;
    }

    public JobFitResult CalculateFit(CvAnalysisDto cvAnalysis, JobPosition? jobPosition)
    {
        var candidateSkills = (cvAnalysis.Skills ?? [])
            .Select(s =>
            {
                var (canonical, _) = _skillNormalizer.Normalize(s.Name);
                return (
                    Original: s,
                    Canonical: string.IsNullOrWhiteSpace(canonical) ? s.Name.Trim() : canonical
                );
            })
            .ToList();

        // 1. If no specific job position, calculate generic profile strength
        if (jobPosition == null || string.IsNullOrWhiteSpace(jobPosition.Requirements))
        {
            return CalculateGenericFit(cvAnalysis, candidateSkills);
        }

        // 2. Extract required skills from JobPosition
        var requiredTokens = jobPosition.Requirements
            .Split(new[] { ',', ';', '\n', '\r', '•', '-' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 2)
            .Select(t =>
            {
                var (canonical, _) = _skillNormalizer.Normalize(t);
                return string.IsNullOrWhiteSpace(canonical) ? t : canonical;
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requiredTokens.Count == 0)
        {
            return CalculateGenericFit(cvAnalysis, candidateSkills);
        }

        var matches = new List<SkillMatchDetail>();
        var missingRequired = new List<string>();
        double skillScoreAccumulator = 0;

        foreach (var req in requiredTokens)
        {
            var matched = candidateSkills.FirstOrDefault(cs =>
                string.Equals(cs.Canonical, req, StringComparison.OrdinalIgnoreCase) ||
                cs.Original.Name.Contains(req, StringComparison.OrdinalIgnoreCase) ||
                req.Contains(cs.Canonical, StringComparison.OrdinalIgnoreCase));

            if (matched.Original != null)
            {
                matches.Add(new SkillMatchDetail(
                    SkillName: req,
                    IsMatched: true,
                    CandidateConfidence: matched.Original.Confidence,
                    ExperienceYears: matched.Original.ExperienceYears,
                    Notes: $"Evidencia: {matched.Original.Evidence}"
                ));

                double weight = matched.Original.Confidence >= 0.8 ? 1.0 : (matched.Original.Confidence >= 0.5 ? 0.75 : 0.5);
                skillScoreAccumulator += weight;
            }
            else
            {
                matches.Add(new SkillMatchDetail(
                    SkillName: req,
                    IsMatched: false,
                    CandidateConfidence: 0,
                    ExperienceYears: 0,
                    Notes: "Requisito no identificado en el expediente"
                ));
                missingRequired.Add(req);
            }
        }

        // Percentage of skills met (up to 70 points)
        double skillPercentage = skillScoreAccumulator / requiredTokens.Count;
        double baseScore = skillPercentage * 70.0;

        // Experience adjustment (up to 15 points)
        double expScore = 0;
        if (cvAnalysis.TotalExperienceYears >= jobPosition.MinExperienceYears)
        {
            expScore = 15;
        }
        else if (jobPosition.MinExperienceYears > 0)
        {
            expScore = Math.Max(0, (cvAnalysis.TotalExperienceYears / jobPosition.MinExperienceYears) * 15.0);
        }
        else
        {
            expScore = 15;
        }

        // Seniority assessment (up to 15 points)
        double seniorityScore = 10;
        string seniorityAssessment;
        string candidateSen = cvAnalysis.EstimatedSeniority?.ToLowerInvariant() ?? "";
        string jobSen = jobPosition.Seniority.ToLowerInvariant();

        if (candidateSen.Contains("senior") || candidateSen.Contains("lead"))
        {
            seniorityScore = 15;
            seniorityAssessment = "Seniority sólido para el puesto requerido.";
        }
        else if (candidateSen.Contains("semi") || candidateSen.Contains("mid"))
        {
            seniorityScore = jobSen.Contains("senior") ? 8 : 15;
            seniorityAssessment = jobSen.Contains("senior")
                ? "Nivel Semi-Senior postulado a vacante Senior. Se sugiere evaluar potencial y autonomía."
                : "Seniority acorde a la posición.";
        }
        else
        {
            seniorityScore = jobSen.Contains("junior") ? 15 : 5;
            seniorityAssessment = "Perfil Junior. Requiere mentoría o supervisión técnica.";
        }

        int finalScore = (int)Math.Round(Math.Clamp(baseScore + expScore + seniorityScore, 10, 99));

        // Identify bonus skills
        var bonusSkills = candidateSkills
            .Where(cs => !matches.Any(m => string.Equals(m.SkillName, cs.Canonical, StringComparison.OrdinalIgnoreCase)))
            .Select(cs => cs.Canonical)
            .Take(5)
            .ToList();

        string fitCategory = finalScore switch
        {
            >= 85 => "Excelente ajuste (Top Match)",
            >= 70 => "Buen ajuste (Calificado)",
            >= 55 => "Ajuste moderado (Requiere validación)",
            _ => "Ajuste bajo con respecto a la vacante"
        };

        return new JobFitResult(
            OverallScore: finalScore,
            Matches: matches,
            MissingRequired: missingRequired,
            BonusSkills: bonusSkills,
            SeniorityAssessment: seniorityAssessment,
            FitCategory: fitCategory);
    }

    private static JobFitResult CalculateGenericFit(CvAnalysisDto cvAnalysis, List<(SkillDto Original, string Canonical)> candidateSkills)
    {
        double score = 50; // baseline

        // Skills quantity & confidence
        if (candidateSkills.Count >= 5) score += 15;
        else score += candidateSkills.Count * 3;

        // Experience
        if (cvAnalysis.TotalExperienceYears >= 5) score += 15;
        else if (cvAnalysis.TotalExperienceYears >= 2) score += 10;
        else score += 5;

        // Certifications & Education
        if ((cvAnalysis.Certifications?.Count ?? 0) > 0) score += 5;
        if ((cvAnalysis.Education?.Count ?? 0) > 0) score += 5;

        int finalScore = (int)Math.Round(Math.Clamp(score, 40, 96));

        string category = finalScore >= 80 ? "Perfil técnico destacado" : "Perfil calificado";

        return new JobFitResult(
            OverallScore: finalScore,
            Matches: candidateSkills.Take(8).Select(s => new SkillMatchDetail(
                SkillName: s.Canonical,
                IsMatched: true,
                CandidateConfidence: s.Original.Confidence,
                ExperienceYears: s.Original.ExperienceYears,
                Notes: s.Original.Evidence
            )).ToList(),
            MissingRequired: [],
            BonusSkills: candidateSkills.Skip(8).Select(s => s.Canonical).ToList(),
            SeniorityAssessment: $"Nivel {cvAnalysis.EstimatedSeniority} ({cvAnalysis.TotalExperienceYears} años de experiencia)",
            FitCategory: category);
    }
}
