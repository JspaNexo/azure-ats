using System.Text.Json;
using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Domain.Common;
using Ats.Domain.Entities;
using Ats.Domain.Enums;

namespace Ats.Application.Features.Reports;

public record GenerateInterviewReportCommand(
    Guid CandidateId,
    Guid EventId,
    Guid CorrelationId);

public class GenerateInterviewReportCommandHandler
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICvAnalysisRepository _cvAnalysisRepository;
    private readonly IDiscRepository _discRepository;
    private readonly IInterviewReportRepository _reportRepository;
    private readonly IProcessingJobRepository _processingJobRepository;
    private readonly IInterviewQuestionGenerator _questionGenerator;
    private readonly IReportDocumentRenderer _documentRenderer;
    private readonly IDocumentStorageService _storageService;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateInterviewReportCommandHandler(
        ICandidateRepository candidateRepository,
        ICvAnalysisRepository cvAnalysisRepository,
        IDiscRepository discRepository,
        IInterviewReportRepository reportRepository,
        IProcessingJobRepository processingJobRepository,
        IInterviewQuestionGenerator questionGenerator,
        IReportDocumentRenderer documentRenderer,
        IDocumentStorageService storageService,
        IUnitOfWork unitOfWork)
    {
        _candidateRepository = candidateRepository;
        _cvAnalysisRepository = cvAnalysisRepository;
        _discRepository = discRepository;
        _reportRepository = reportRepository;
        _processingJobRepository = processingJobRepository;
        _questionGenerator = questionGenerator;
        _documentRenderer = documentRenderer;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InterviewReportDto>> HandleAsync(GenerateInterviewReportCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Check if candidate already has a generated report (Cache / Token Guard)
        var existingReport = await _reportRepository.GetByCandidateIdAsync(command.CandidateId, cancellationToken);
        if (existingReport is not null && existingReport.Status == ReportStatus.Generated && !string.IsNullOrEmpty(existingReport.ReportContentJson))
        {
            var cachedDto = JsonSerializer.Deserialize<InterviewReportDto>(existingReport.ReportContentJson);
            if (cachedDto is not null) return Result.Success(cachedDto);
        }

        // 2. Idempotency check via ProcessingJob
        var existingJob = await _processingJobRepository.GetByEventIdAsync(command.EventId, cancellationToken);
        if (existingJob is not null && existingJob.Status == ProcessingStatus.Processed)
        {
            if (existingReport is not null && !string.IsNullOrEmpty(existingReport.ReportContentJson))
            {
                var cachedDto = JsonSerializer.Deserialize<InterviewReportDto>(existingReport.ReportContentJson);
                if (cachedDto is not null) return Result.Success(cachedDto);
            }
        }

        var job = existingJob ?? ProcessingJob.Create(command.CandidateId, ProcessType.InterviewReportGeneration, command.EventId, command.CorrelationId);
        if (existingJob is null) await _processingJobRepository.AddAsync(job, cancellationToken);

        job.MarkAsProcessing();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 2. Fetch Candidate, CV Analysis, and DISC Interpretation
        var candidate = await _candidateRepository.GetByIdAsync(command.CandidateId, cancellationToken);
        if (candidate is null)
        {
            job.MarkAsFailed("Candidate.NotFound", "Candidato no encontrado.");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<InterviewReportDto>(Error.NotFound("Candidate.NotFound", "Candidato no encontrado."));
        }

        var cvAnalysis = await _cvAnalysisRepository.GetByCandidateIdAsync(command.CandidateId, cancellationToken);
        if (cvAnalysis is null || cvAnalysis.Status != ProcessingStatus.Processed)
        {
            job.MarkAsFailed("CvAnalysis.NotReady", "El análisis de CV aún no está procesado.");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<InterviewReportDto>(Error.Validation("CvAnalysis.NotReady", "El análisis de CV aún no está procesado."));
        }

        var discInterpretation = await _discRepository.GetInterpretationByCandidateIdAsync(command.CandidateId, cancellationToken);
        if (discInterpretation is null || discInterpretation.Status != ProcessingStatus.Processed)
        {
            job.MarkAsFailed("DiscInterpretation.NotReady", "La interpretación DISC aún no está procesada.");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<InterviewReportDto>(Error.Validation("DiscInterpretation.NotReady", "La interpretación DISC aún no está procesada."));
        }

        var cvDto = JsonSerializer.Deserialize<CvAnalysisDto>(cvAnalysis.AnalysisJson);
        var discDto = JsonSerializer.Deserialize<DiscInterpretationDto>(discInterpretation.InterpretationJson);

        if (cvDto is null || discDto is null)
        {
            job.MarkAsFailed("Serialization.Error", "Error deserializando datos procesados.");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<InterviewReportDto>(Error.Failure("Serialization.Error", "Error procesando datos consolidados."));
        }

        // 3. Generate Interview Questions
        var questionsResult = await _questionGenerator.GenerateQuestionsAsync(cvDto, discDto, cancellationToken);
        if (questionsResult.IsFailure)
        {
            job.MarkAsFailed(questionsResult.Error.Code, questionsResult.Error.Description);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<InterviewReportDto>(questionsResult.Error);
        }

        // 4. Assemble Consolidated 2-Page Report DTO
        var reportDto = new InterviewReportDto(
            CandidateOverview: new CandidateOverviewDto(
                Name: $"{candidate.FirstName} {candidate.LastName}",
                CurrentRole: cvDto.CurrentRole,
                ExperienceYears: cvDto.TotalExperienceYears,
                ProfessionalSummary: cvDto.ProfessionalSummary),
            ProfessionalProfile: new ProfessionalProfileDto(
                MainSkills: (cvDto.Skills ?? []).Select(s => s.NormalizedName).Take(8).ToList(),
                RelevantExperience: (cvDto.WorkExperience ?? []).Select(w => $"{w.Role} en {w.Company} ({w.DurationYears} años)").Take(4).ToList(),
                Education: (cvDto.Education ?? []).Select(e => $"{e.Degree} - {e.Institution}").ToList(),
                Languages: (cvDto.Languages ?? []).Select(l => $"{l.Name} ({l.Level})").ToList(),
                Certifications: (cvDto.Certifications ?? []).Select(c => c.Name).ToList()),
            DiscSummary: new DiscSummaryDto(
                PrimaryStyle: discDto.PrimaryStyle,
                Summary: discDto.Summary,
                StrengthsToExplore: discDto.StrengthsToExplore ?? [],
                PointsToExplore: discDto.PointsToExplore ?? []),
            ValidationPoints: (cvDto.PointsToValidate ?? []).Select(p => new ValidationPointDto(
                Topic: "Skill / Experiencia",
                Reason: p,
                Source: "CV")).ToList(),
            InterviewGuide: new InterviewGuideDto(
                ProfessionalQuestions: questionsResult.Value.ProfessionalQuestions ?? [],
                TechnicalQuestions: questionsResult.Value.TechnicalQuestions ?? [],
                BehavioralQuestions: questionsResult.Value.BehavioralQuestions ?? []),
            Disclaimer: "Este informe sirve como herramienta de apoyo para la entrevista y no reemplaza el criterio profesional del reclutador ni realiza diagnósticos psicológicos.");

        // 5. Render PDF Document
        var pdfResult = await _documentRenderer.RenderReportPdfAsync(reportDto, cancellationToken);
        string? fileUrl = null;
        if (pdfResult.IsSuccess && pdfResult.Value.Length > 0)
        {
            using var memoryStream = new MemoryStream(pdfResult.Value);
            fileUrl = await _storageService.SaveFileAsync(
                memoryStream,
                $"informe_preentrevista_{candidate.Id}.pdf",
                "application/pdf",
                cancellationToken);
        }

        reportDto = reportDto with { FileUrl = fileUrl };

        // 6. Persist Report Entity
        var report = await _reportRepository.GetByCandidateIdAsync(command.CandidateId, cancellationToken)
            ?? InterviewReport.CreateInitial(command.CandidateId);

        string reportJson = JsonSerializer.Serialize(reportDto);
        report.MarkAsGenerated(
            cvAnalysis.Id,
            discInterpretation.Id,
            reportJson,
            fileUrl,
            "GoogleGemini",
            "gemini-1.5-flash",
            "v1.0");

        if (report.Id == Guid.Empty || await _reportRepository.GetByIdAsync(report.Id, cancellationToken) is null)
        {
            await _reportRepository.AddAsync(report, cancellationToken);
        }
        else
        {
            _reportRepository.Update(report);
        }

        job.MarkAsProcessed();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(reportDto);
    }
}

public record GetInterviewReportQuery(Guid CandidateId);

public class GetInterviewReportQueryHandler
{
    private readonly IInterviewReportRepository _reportRepository;

    public GetInterviewReportQueryHandler(IInterviewReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<InterviewReportDto>> HandleAsync(GetInterviewReportQuery query, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByCandidateIdAsync(query.CandidateId, cancellationToken);
        if (report is null)
        {
            return Result.Failure<InterviewReportDto>(
                Error.NotFound("Report.NotFound", $"No se encontró informe preentrevista para el candidato {query.CandidateId}."));
        }

        if (report.Status != ReportStatus.Generated || string.IsNullOrEmpty(report.ReportContentJson))
        {
            return Result.Failure<InterviewReportDto>(
                Error.Validation("Report.NotGenerated", $"El informe está en estado '{report.Status}' y aún no ha finalizado su generación."));
        }

        var dto = JsonSerializer.Deserialize<InterviewReportDto>(report.ReportContentJson);
        if (dto is null)
        {
            return Result.Failure<InterviewReportDto>(
                Error.Failure("Report.SerializationError", "Error deserializando el contenido del informe."));
        }

        return Result.Success(dto);
    }
}


public record GetReportPdfQuery(Guid CandidateId);

public class GetReportPdfQueryHandler
{
    private readonly IDocumentStorageService _storageService;
    private readonly IInterviewReportRepository _reportRepository;
    private readonly IReportDocumentRenderer _documentRenderer;

    public GetReportPdfQueryHandler(
        IDocumentStorageService storageService,
        IInterviewReportRepository reportRepository,
        IReportDocumentRenderer documentRenderer)
    {
        _storageService = storageService;
        _reportRepository = reportRepository;
        _documentRenderer = documentRenderer;
    }

    public async Task<Result<byte[]>> HandleAsync(GetReportPdfQuery query, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByCandidateIdAsync(query.CandidateId, cancellationToken);
        if (report is null)
        {
            return Result.Failure<byte[]>(Error.NotFound("Report.NotFound", "No se ha generado un informe preentrevista para este candidato."));
        }

        if (!string.IsNullOrWhiteSpace(report.FileUrl))
        {
            var existingStream = await _storageService.GetFileAsync(report.FileUrl, cancellationToken);
            if (existingStream is not null)
            {
                using var memoryStream = new MemoryStream();
                await existingStream.CopyToAsync(memoryStream, cancellationToken);
                return Result<byte[]>.Success(memoryStream.ToArray());
            }
        }

        if (!string.IsNullOrWhiteSpace(report.ReportContentJson))
        {
            var dto = JsonSerializer.Deserialize<InterviewReportDto>(report.ReportContentJson);
            if (dto is not null)
            {
                var renderResult = await _documentRenderer.RenderReportPdfAsync(dto, cancellationToken);
                if (renderResult.IsSuccess && renderResult.Value.Length > 0)
                {
                    return Result<byte[]>.Success(renderResult.Value);
                }
            }
        }

        return Result.Failure<byte[]>(Error.Failure("Report.RenderFailed", "No se pudo generar el documento PDF del informe."));
    }
}

