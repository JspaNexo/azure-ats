using Ats.Application.DTOs;

namespace Ats.Application.Features.CvProcessing;

public static class CvConsistencyChecker
{
    public static List<string> CheckConsistency(CvAnalysisDto analysis)
    {
        var warnings = new List<string>();

        if (analysis == null)
        {
            return warnings;
        }

        // 1. Discrepancia entre suma de duraciones y años de experiencia totales declarados
        if (analysis.WorkExperience != null && analysis.WorkExperience.Count > 0)
        {
            double sumDurations = analysis.WorkExperience.Sum(w => Math.Max(0, w.DurationYears));
            if (sumDurations > 0 && Math.Abs(sumDurations - analysis.TotalExperienceYears) > 3.0)
            {
                warnings.Add($"Discrepancia temporal: La suma de duraciones de los empleos ({sumDurations:F1} años) difiere significativamente del total de experiencia estimado ({analysis.TotalExperienceYears:F1} años).");
            }
        }

        // 2. Habilidad técnica con más años de experiencia que los años totales del candidato
        if (analysis.Skills != null)
        {
            foreach (var skill in analysis.Skills)
            {
                if (skill.ExperienceYears > analysis.TotalExperienceYears + 1.0 && analysis.TotalExperienceYears > 0)
                {
                    warnings.Add($"Inconsistencia de experiencia: La habilidad '{skill.Name}' reporta {skill.ExperienceYears} años de experiencia, superando el total de trayectoria del candidato ({analysis.TotalExperienceYears} años).");
                }
            }

            // 3. Perfil inflado: Demasiadas tecnologías para nivel inicial
            int skillCount = analysis.Skills.Count;
            if (analysis.TotalExperienceYears < 2.0 && skillCount > 12)
            {
                warnings.Add($"Alerta de consistencia: Se listan {skillCount} habilidades técnicas para una trayectoria de {analysis.TotalExperienceYears:F1} años (posible perfil sobreestimado).");
            }
        }

        // 4. Fechas de graduación o certificaciones anómalas
        int currentYear = DateTime.UtcNow.Year;
        if (analysis.Education != null)
        {
            foreach (var edu in analysis.Education)
            {
                if (edu.GraduationYear > currentYear + 6 || (edu.GraduationYear > 0 && edu.GraduationYear < 1960))
                {
                    warnings.Add($"Inconsistencia en formación: El año de graduación {edu.GraduationYear} en '{edu.Degree}' está fuera del rango temporal esperado.");
                }
            }
        }

        if (analysis.Certifications != null)
        {
            foreach (var cert in analysis.Certifications)
            {
                if (cert.Year > currentYear + 1 || (cert.Year > 0 && cert.Year < 1980))
                {
                    warnings.Add($"Inconsistencia en certificación: El año {cert.Year} reportado en '{cert.Name}' no corresponde a una fecha contemporánea válida.");
                }
            }
        }

        return warnings;
    }
}
