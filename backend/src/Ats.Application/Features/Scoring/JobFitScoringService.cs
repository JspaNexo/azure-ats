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

        // 2. Extract and sanitize required skills from JobPosition
        var requiredTokens = jobPosition.Requirements
            .Split(new[] { ',', ';', '\n', '\r', '•', '-' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.Trim().TrimEnd('.', ';', ':', ',', '!'))
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

                double conf = Math.Clamp(matched.Original.Confidence, 0.5, 1.0);
                double expRatio = jobPosition.MinExperienceYears > 0
                    ? Math.Clamp((double)matched.Original.ExperienceYears / Math.Max(1, jobPosition.MinExperienceYears - 1), 0.5, 1.2)
                    : 1.0;
                double weight = Math.Clamp(0.75 + (conf * 0.20) + (Math.Min(1.0, expRatio) * 0.05), 0.75, 1.0);
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

        // 1. Percentage of skills met (up to 70 points)
        double skillPercentage = skillScoreAccumulator / requiredTokens.Count;
        double baseScore = skillPercentage * 70.0;

        // 2. Identify bonus skills not in core requirements (up to 5 points)
        var bonusSkills = candidateSkills
            .Where(cs => !matches.Any(m => m.IsMatched && (string.Equals(m.SkillName, cs.Canonical, StringComparison.OrdinalIgnoreCase) || cs.Canonical.Contains(m.SkillName, StringComparison.OrdinalIgnoreCase))))
            .Select(cs => cs.Canonical)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
        double bonusScore = Math.Min(5.0, bonusSkills.Count * 1.0);

        // 3. Experience adjustment (up to 15 points)
        double expScore;
        if (jobPosition.MinExperienceYears > 0)
        {
            if (cvAnalysis.TotalExperienceYears >= jobPosition.MinExperienceYears)
            {
                double extraYears = cvAnalysis.TotalExperienceYears - jobPosition.MinExperienceYears;
                expScore = Math.Min(15.0, 12.0 + Math.Min(3.0, extraYears * 0.75));
            }
            else
            {
                expScore = Math.Max(1.0, (cvAnalysis.TotalExperienceYears / (double)jobPosition.MinExperienceYears) * 11.0);
            }
        }
        else
        {
            expScore = Math.Min(15.0, 8.0 + (cvAnalysis.TotalExperienceYears * 1.5));
        }

        // 4. Seniority assessment (up to 10 points)
        double seniorityScore;
        string seniorityAssessment;
        string candidateSen = cvAnalysis.EstimatedSeniority?.ToLowerInvariant() ?? "";
        string jobSen = jobPosition.Seniority.ToLowerInvariant();

        if (candidateSen.Contains("lead"))
        {
            seniorityScore = 10.0;
            seniorityAssessment = "Perfil Lead con liderazgo técnico demostrado.";
        }
        else if (candidateSen.Contains("senior"))
        {
            seniorityScore = jobSen.Contains("lead") ? 7.5 : 10.0;
            seniorityAssessment = "Seniority sólido para el puesto requerido.";
        }
        else if (candidateSen.Contains("semi") || candidateSen.Contains("mid"))
        {
            seniorityScore = jobSen.Contains("senior") || jobSen.Contains("lead") ? 5.5 : 9.5;
            seniorityAssessment = jobSen.Contains("senior") || jobSen.Contains("lead")
                ? "Nivel Semi-Senior postulado a vacante Senior. Se sugiere evaluar potencial y autonomía."
                : "Seniority acorde a la posición.";
        }
        else
        {
            seniorityScore = jobSen.Contains("junior") ? 9.5 : 2.5;
            seniorityAssessment = "Perfil Junior. Requiere mentoría o supervisión técnica.";
        }

        // 5. Education & Certifications bonus (up to 5 points)
        double educationCertScore = 0;
        if ((cvAnalysis.Certifications?.Count ?? 0) > 0) educationCertScore += Math.Min(3.0, (cvAnalysis.Certifications?.Count ?? 0) * 1.5);
        if ((cvAnalysis.Education?.Count ?? 0) > 0) educationCertScore += 2.0;

        int finalScore = (int)Math.Round(Math.Clamp(baseScore + bonusScore + expScore + seniorityScore + educationCertScore, 10, 99));

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
        double baseScore = 40.0;

        // Skills count & confidence
        double avgConf = candidateSkills.Any() ? candidateSkills.Average(s => s.Original.Confidence) : 0.7;
        double skillsScore = Math.Min(25.0, candidateSkills.Count * 2.5 * avgConf);

        // Experience depth
        double expScore = Math.Min(20.0, cvAnalysis.TotalExperienceYears * 2.5);

        // Seniority
        string sen = cvAnalysis.EstimatedSeniority?.ToLowerInvariant() ?? "";
        double senScore = sen.Contains("lead") ? 10.0 : (sen.Contains("senior") ? 8.0 : (sen.Contains("semi") ? 5.0 : 3.0));

        // Education & Certifications
        double certScore = Math.Min(3.0, (cvAnalysis.Certifications?.Count ?? 0) * 1.5);
        double eduScore = Math.Min(2.0, (cvAnalysis.Education?.Count ?? 0) * 1.0);

        int finalScore = (int)Math.Round(Math.Clamp(baseScore + skillsScore + expScore + senScore + certScore + eduScore, 35, 96));

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
