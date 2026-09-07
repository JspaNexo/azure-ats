using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Domain.Common;

namespace Ats.Infrastructure.Services.Reporting;

public class ReportDocumentRenderer : IReportDocumentRenderer
{
    public Task<Result<byte[]>> RenderReportPdfAsync(InterviewReportDto reportDto, CancellationToken cancellationToken = default)
    {
        var builder = new PdfDocumentBuilder();
        var regularFont = builder.AddStandard14Font(Standard14Font.Helvetica);
        var boldFont = builder.AddStandard14Font(Standard14Font.HelveticaBold);

        // ==========================================
        // PÁGINA 1: PERFIL PROFESIONAL Y SÍNTESIS DISC
        // ==========================================
        var page1 = builder.AddPage(PageSize.A4);
        
        page1.AddText("INFORME PREENTREVISTA - ATS", 16, new PdfPoint(50, 790), boldFont);
        page1.AddText($"Candidato: {SanitizeText(reportDto.CandidateOverview.Name)}", 12, new PdfPoint(50, 765), boldFont);
        page1.AddText($"Cargo Actual: {SanitizeText(reportDto.CandidateOverview.CurrentRole)} | Exp: {reportDto.CandidateOverview.ExperienceYears} anos", 10, new PdfPoint(50, 748), regularFont);

        // Resumen Profesional
        page1.AddText("1. RESUMEN PROFESIONAL", 11, new PdfPoint(50, 720), boldFont);
        page1.AddText(SanitizeText(reportDto.CandidateOverview.ProfessionalSummary), 9, new PdfPoint(50, 703), regularFont);

        // Skills Principales
        page1.AddText("2. SKILLS PRINCIPALES DETECTADAS EN CV", 11, new PdfPoint(50, 670), boldFont);
        int skillY = 653;
        foreach (var skill in reportDto.ProfessionalProfile.MainSkills.Take(6))
        {
            page1.AddText($"- {SanitizeText(skill)}", 9, new PdfPoint(60, skillY), regularFont);
            skillY -= 14;
        }

        // Experiencia Relevante
        page1.AddText("3. EXPERIENCIA LABORAL RELEVANTE", 11, new PdfPoint(50, skillY - 10), boldFont);
        int expY = skillY - 27;
        foreach (var exp in reportDto.ProfessionalProfile.RelevantExperience.Take(3))
        {
            page1.AddText($"- {SanitizeText(exp)}", 9, new PdfPoint(60, expY), regularFont);
            expY -= 14;
        }

        // Síntesis DISC
        page1.AddText($"4. SINTESIS CONDUCTUAL DISC (Estilo {SanitizeText(reportDto.DiscSummary.PrimaryStyle)})", 11, new PdfPoint(50, expY - 15), boldFont);
        page1.AddText(SanitizeText(reportDto.DiscSummary.Summary), 9, new PdfPoint(50, expY - 32), regularFont);

        page1.AddText("Fortalezas conductuales para explorar:", 9, new PdfPoint(50, expY - 52), boldFont);
        int discY = expY - 67;
        foreach (var str in reportDto.DiscSummary.StrengthsToExplore.Take(2))
        {
            page1.AddText($"* {SanitizeText(str)}", 9, new PdfPoint(60, discY), regularFont);
            discY -= 14;
        }

        page1.AddText("Puntos a explorar ante presion/cambios:", 9, new PdfPoint(50, discY - 5), boldFont);
        discY -= 20;
        foreach (var pt in reportDto.DiscSummary.PointsToExplore.Take(2))
        {
            page1.AddText($"* {SanitizeText(pt)}", 9, new PdfPoint(60, discY), regularFont);
            discY -= 14;
        }

        page1.AddText("Pagina 1 de 2 - ATS Pre-Interview Report System", 8, new PdfPoint(50, 30), regularFont);

        // ==========================================
        // PÁGINA 2: GUÍA ESTRUCTURADA PARA LA ENTREVISTA
        // ==========================================
        var page2 = builder.AddPage(PageSize.A4);
        page2.AddText("GUIA ESTRUCTURADA PARA LA ENTREVISTA", 16, new PdfPoint(50, 790), boldFont);
        page2.AddText($"Candidato: {SanitizeText(reportDto.CandidateOverview.Name)} | Estilo DISC: {SanitizeText(reportDto.DiscSummary.PrimaryStyle)}", 10, new PdfPoint(50, 768), regularFont);

        // Puntos a Validar
        page2.AddText("1. PUNTOS A VALIDAR O PROFUNDIZAR", 11, new PdfPoint(50, 735), boldFont);
        int valY = 718;
        foreach (var vp in reportDto.ValidationPoints.Take(2))
        {
            page2.AddText($"[!] {SanitizeText(vp.Reason)}", 9, new PdfPoint(60, valY), regularFont);
            valY -= 14;
        }

        // Preguntas Profesionales
        page2.AddText("2. PREGUNTAS PROFESIONALES (TRAYECTORIA)", 11, new PdfPoint(50, valY - 10), boldFont);
        int profY = valY - 27;
        for (int i = 0; i < reportDto.InterviewGuide.ProfessionalQuestions.Count && i < 2; i++)
        {
            page2.AddText($"{i + 1}. {SanitizeText(reportDto.InterviewGuide.ProfessionalQuestions[i])}", 9, new PdfPoint(60, profY), regularFont);
            profY -= 16;
        }

        // Preguntas Técnicas
        page2.AddText("3. PREGUNTAS TECNICAS (SKILLS & ARQUITECTURA)", 11, new PdfPoint(50, profY - 10), boldFont);
        int techY = profY - 27;
        for (int i = 0; i < reportDto.InterviewGuide.TechnicalQuestions.Count && i < 3; i++)
        {
            page2.AddText($"{i + 1}. {SanitizeText(reportDto.InterviewGuide.TechnicalQuestions[i])}", 9, new PdfPoint(60, techY), regularFont);
            techY -= 16;
        }

        // Preguntas Conductuales STAR
        page2.AddText("4. PREGUNTAS CONDUCTUALES (METODOLOGIA STAR)", 11, new PdfPoint(50, techY - 10), boldFont);
        int starY = techY - 27;
        for (int i = 0; i < reportDto.InterviewGuide.BehavioralQuestions.Count && i < 2; i++)
        {
            page2.AddText($"{i + 1}. {SanitizeText(reportDto.InterviewGuide.BehavioralQuestions[i])}", 9, new PdfPoint(60, starY), regularFont);
            starY -= 16;
        }

        // Notas del Entrevistador
        page2.AddText("5. NOTAS Y EVALUACION DEL ENTREVISTADOR", 11, new PdfPoint(50, starY - 15), boldFont);
        page2.AddText("[  Fortalezas observadas: ___________________________________________________________  ]", 8, new PdfPoint(50, starY - 35), regularFont);
        page2.AddText("[  Aspectos de mejora:    ___________________________________________________________  ]", 8, new PdfPoint(50, starY - 50), regularFont);
        page2.AddText("[  Decision recomendada:  [ ] Avanzar a oferta   [ ] Segunda entrevista   [ ] Descartar      ]", 8, new PdfPoint(50, starY - 65), regularFont);

        // Disclaimer
        page2.AddText("AVISO DE USO RESPONSABLE:", 8, new PdfPoint(50, 60), boldFont);
        page2.AddText(SanitizeText(reportDto.Disclaimer), 7, new PdfPoint(50, 48), regularFont);
        page2.AddText("Pagina 2 de 2 - ATS Pre-Interview Report System", 8, new PdfPoint(50, 30), regularFont);

        byte[] pdfBytes = builder.Build();
        return Task.FromResult(Result.Success(pdfBytes));
    }

    private static string SanitizeText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();

        foreach (var c in normalized)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                char cleanChar = c switch
                {
                    '¿' or '¡' => ' ',
                    '“' or '”' or '«' or '»' => '"',
                    '’' or '‘' or '`' => '\'',
                    '—' or '–' => '-',
                    '…' => '.',
                    _ => c
                };

                if (cleanChar >= 32 && cleanChar <= 126)
                {
                    sb.Append(cleanChar);
                }
                else if (cleanChar == '\n' || cleanChar == '\r' || cleanChar == '\t')
                {
                    sb.Append(' ');
                }
            }
        }

        return sb.ToString();
    }
}

