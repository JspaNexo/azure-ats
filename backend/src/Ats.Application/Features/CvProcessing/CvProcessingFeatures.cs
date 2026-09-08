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
        // 1. Check if candidate already has a processed CV analysis for this document (Cache / Token Guard)
        var existingAnalysis = await _cvAnalysisRepository.GetByCandidateIdAsync(command.CandidateId, cancellationToken);
        if (existingAnalysis is not null && existingAnalysis.DocumentId == command.DocumentId && existingAnalysis.Status == ProcessingStatus.Processed && !string.IsNullOrEmpty(existingAnalysis.AnalysisJson))
        {
            var cachedDto = JsonSerializer.Deserialize<CvAnalysisDto>(existingAnalysis.AnalysisJson);
            if (cachedDto is not null) return Result.Success(cachedDto);
        }

        // 2. Idempotency check via ProcessingJob
        var existingJob = await _processingJobRepository.GetByEventIdAsync(command.EventId, cancellationToken);
        if (existingJob is not null && existingJob.Status == ProcessingStatus.Processed)
        {
            if (existingAnalysis is not null && existingAnalysis.DocumentId == command.DocumentId && !string.IsNullOrEmpty(existingAnalysis.AnalysisJson))
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

        // 3. Retrieve PDF stream and copy to memory to allow multiple reads
        using var stream = await _storageService.GetFileAsync(document.StoragePath, cancellationToken);
        if (stream is null)
        {
            job.MarkAsFailed("Storage.FileNotFound", "No se pudo recuperar el archivo PDF del almacenamiento.");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<CvAnalysisDto>(Error.NotFound("Storage.FileNotFound", "Archivo PDF no encontrado."));
        }

        using var memStream = new MemoryStream();
        await stream.CopyToAsync(memStream, cancellationToken);
        memStream.Seek(0, SeekOrigin.Begin);

        // 4. Primary: Direct Multimodal PDF Analysis with Gemini
        var analysisResult = await _cvAnalyzer.AnalyzeCvFromPdfAsync(memStream, document.FileName, cancellationToken);

        // 5. Fallback: If multimodal analysis fails, fallback to raw text extraction via PdfPig
        if (analysisResult.IsFailure)
        {
            memStream.Seek(0, SeekOrigin.Begin);
            string extractedText = await _pdfTextExtractor.ExtractTextAsync(memStream, cancellationToken);
            if (!string.IsNullOrWhiteSpace(extractedText) && extractedText.Length >= 50)
            {
                analysisResult = await _cvAnalyzer.AnalyzeCvTextAsync(extractedText, cancellationToken);
            }
        }

        if (analysisResult.IsFailure)
        {
            job.MarkAsFailed(analysisResult.Error.Code, analysisResult.Error.Description);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<CvAnalysisDto>(analysisResult.Error);
        }

        // 6. Cross-Validation: Temporal & Experience Consistency Checks
        var consistencyWarnings = CvConsistencyChecker.CheckConsistency(analysisResult.Value);
        if (consistencyWarnings.Count > 0)
        {
            var combinedWarnings = new List<string>(analysisResult.Value.Warnings ?? []);
            combinedWarnings.AddRange(consistencyWarnings);
            analysisResult = Result.Success(analysisResult.Value with { Warnings = combinedWarnings.Distinct().ToList() });
        }

        var cvAnalysis = await _cvAnalysisRepository.GetByCandidateIdAsync(command.CandidateId, cancellationToken);
        if (cvAnalysis is null)
        {
            cvAnalysis = CvAnalysis.CreatePending(command.CandidateId, command.DocumentId, "GoogleGemini", "gemini-1.5-flash", "v1.0");
        }
        else
        {
            cvAnalysis.UpdateDocument(command.DocumentId);
        }

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

public record RecordCvFeedbackCommand(Guid CandidateId, CvFeedbackDto Feedback);

public class RecordCvFeedbackCommandHandler
{
    private readonly ICvAnalysisRepository _cvAnalysisRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RecordCvFeedbackCommandHandler(
        ICvAnalysisRepository cvAnalysisRepository,
        IUnitOfWork unitOfWork)
    {
        _cvAnalysisRepository = cvAnalysisRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(RecordCvFeedbackCommand command, CancellationToken cancellationToken = default)
    {
        var cvAnalysis = await _cvAnalysisRepository.GetByCandidateIdAsync(command.CandidateId, cancellationToken);
        if (cvAnalysis == null)
        {
            return Result.Failure(Error.NotFound("CvAnalysis.NotFound", $"No se encontró análisis de CV para el candidato {command.CandidateId}."));
        }

        string feedbackJson = JsonSerializer.Serialize(command.Feedback);
        cvAnalysis.RecordEvaluatorFeedback(feedbackJson);
        _cvAnalysisRepository.Update(cvAnalysis);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

