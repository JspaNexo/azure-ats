using System.Text.Json;
using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Domain.Common;
using Ats.Domain.Entities;
using Ats.Domain.Enums;

namespace Ats.Application.Features.CvProcessing;

public record ProcessCvAnalysisCommand(
    Guid CandidateId,
    Guid DocumentId,
    Guid EventId,
    Guid CorrelationId);

public class ProcessCvAnalysisCommandHandler
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICvAnalysisRepository _cvAnalysisRepository;
    private readonly IProcessingJobRepository _processingJobRepository;
    private readonly IDocumentStorageService _storageService;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly ICvAnalyzer _cvAnalyzer;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessCvAnalysisCommandHandler(
        ICandidateRepository candidateRepository,
        ICvAnalysisRepository cvAnalysisRepository,
        IProcessingJobRepository processingJobRepository,
        IDocumentStorageService storageService,
        IPdfTextExtractor pdfTextExtractor,
        ICvAnalyzer cvAnalyzer,
        IUnitOfWork unitOfWork)
    {
        _candidateRepository = candidateRepository;
        _cvAnalysisRepository = cvAnalysisRepository;
        _processingJobRepository = processingJobRepository;
        _storageService = storageService;
        _pdfTextExtractor = pdfTextExtractor;
        _cvAnalyzer = cvAnalyzer;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CvAnalysisDto>> HandleAsync(ProcessCvAnalysisCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Check if candidate already has a processed CV analysis (Cache / Token Guard)
        var existingAnalysis = await _cvAnalysisRepository.GetByCandidateIdAsync(command.CandidateId, cancellationToken);
        if (existingAnalysis is not null && existingAnalysis.Status == ProcessingStatus.Processed && !string.IsNullOrEmpty(existingAnalysis.AnalysisJson))
        {
            var cachedDto = JsonSerializer.Deserialize<CvAnalysisDto>(existingAnalysis.AnalysisJson);
            if (cachedDto is not null) return Result.Success(cachedDto);
        }

        // 2. Idempotency check via ProcessingJob
        var existingJob = await _processingJobRepository.GetByEventIdAsync(command.EventId, cancellationToken);
        if (existingJob is not null && existingJob.Status == ProcessingStatus.Processed)
        {
            if (existingAnalysis is not null && !string.IsNullOrEmpty(existingAnalysis.AnalysisJson))
            {
                var cachedDto = JsonSerializer.Deserialize<CvAnalysisDto>(existingAnalysis.AnalysisJson);
                if (cachedDto is not null) return Result.Success(cachedDto);
            }
        }

        var job = existingJob ?? ProcessingJob.Create(command.CandidateId, ProcessType.CvAnalysis, command.EventId, command.CorrelationId);
        if (existingJob is null) await _processingJobRepository.AddAsync(job, cancellationToken);

        job.MarkAsProcessing();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 2. Fetch candidate and document
        var candidate = await _candidateRepository.GetByIdAsync(command.CandidateId, cancellationToken);
        if (candidate is null)
        {
            job.MarkAsFailed("Candidate.NotFound", $"Candidato {command.CandidateId} no encontrado.");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<CvAnalysisDto>(Error.NotFound("Candidate.NotFound", "Candidato no encontrado."));
        }

        var document = candidate.Documents.FirstOrDefault(d => d.Id == command.DocumentId);
        if (document is null)
        {
            job.MarkAsFailed("Document.NotFound", $"Documento {command.DocumentId} no encontrado.");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<CvAnalysisDto>(Error.NotFound("Document.NotFound", "Documento no encontrado."));
        }

        // 3. Extract text from PDF
        using var stream = await _storageService.GetFileAsync(document.StoragePath, cancellationToken);
        if (stream is null)
        {
            job.MarkAsFailed("Storage.FileNotFound", "No se pudo recuperar el archivo PDF del almacenamiento.");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<CvAnalysisDto>(Error.NotFound("Storage.FileNotFound", "Archivo PDF no encontrado."));
        }

        string extractedText = await _pdfTextExtractor.ExtractTextAsync(stream, cancellationToken);
        if (string.IsNullOrWhiteSpace(extractedText) || extractedText.Length < 50)
        {
            job.MarkAsFailed("Pdf.NoText", "El archivo PDF no contiene suficiente texto extraíble.");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<CvAnalysisDto>(Error.Validation("Pdf.NoText", "El archivo PDF no contiene texto extraíble."));
        }

        // 4. Analyze CV with AI
        var analysisResult = await _cvAnalyzer.AnalyzeCvTextAsync(extractedText, cancellationToken);
        if (analysisResult.IsFailure)
        {
            job.MarkAsFailed(analysisResult.Error.Code, analysisResult.Error.Description);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<CvAnalysisDto>(analysisResult.Error);
        }

        var cvAnalysis = await _cvAnalysisRepository.GetByCandidateIdAsync(command.CandidateId, cancellationToken)
            ?? CvAnalysis.CreatePending(command.CandidateId, command.DocumentId, "GoogleGemini", "gemini-1.5-flash", "v1.0");

        string jsonContent = JsonSerializer.Serialize(analysisResult.Value);
        cvAnalysis.MarkAsProcessed(jsonContent);

        if (cvAnalysis.Id == Guid.Empty || await _cvAnalysisRepository.GetByIdAsync(cvAnalysis.Id, cancellationToken) is null)
        {
            await _cvAnalysisRepository.AddAsync(cvAnalysis, cancellationToken);
        }
        else
        {
            _cvAnalysisRepository.Update(cvAnalysis);
        }

        job.MarkAsProcessed();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(analysisResult.Value);
    }
}

