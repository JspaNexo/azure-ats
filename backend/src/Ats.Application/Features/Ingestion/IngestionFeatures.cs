using FluentValidation;
using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Application.Features.Candidates;
using Ats.Application.Features.CvProcessing;
using Ats.Application.Features.Disc;
using Ats.Application.Features.Documents;
using Ats.Application.Features.Reports;
using Ats.Domain.Common;

namespace Ats.Application.Features.Ingestion;

public record IngestCandidateCommand(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    Stream CvStream,
    string FileName,
    long FileSizeBytes,
    string ContentType,
    int Dominance,
    int Influence,
    int Steadiness,
    int Conscientiousness,
    string? PrimaryStyle,
    string? TargetRole = null,
    Guid? JobPositionId = null,
    bool Asynchronous = false);

public class IngestCandidateCommandValidator : AbstractValidator<IngestCandidateCommand>
{
    private static readonly string[] AllowedExtensions = [".pdf"];
    private const long MaxFileSizeBytes = 15 * 1024 * 1024; // 15 MB

    public IngestCandidateCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("El nombre es obligatorio.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("El apellido es obligatorio.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Correo electrónico no válido.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("El nombre del archivo CV es obligatorio.")
            .Must(HaveValidExtension).WithMessage("Solo se admiten archivos PDF.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("El archivo PDF no puede estar vacío.")
            .LessThanOrEqualTo(MaxFileSizeBytes).WithMessage("El archivo no puede exceder los 15 MB.");

        RuleFor(x => x.Dominance).InclusiveBetween(0, 100).WithMessage("D debe estar entre 0 y 100.");
        RuleFor(x => x.Influence).InclusiveBetween(0, 100).WithMessage("I debe estar entre 0 y 100.");
        RuleFor(x => x.Steadiness).InclusiveBetween(0, 100).WithMessage("S debe estar entre 0 y 100.");
        RuleFor(x => x.Conscientiousness).InclusiveBetween(0, 100).WithMessage("C debe estar entre 0 y 100.");
    }

    private bool HaveValidExtension(string fileName)
    {
        string extension = Path.GetExtension(fileName);
        return AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}

public class IngestCandidateCommandHandler
{
    private readonly RegisterCandidateCommandHandler _registerHandler;
    private readonly UploadCvCommandHandler _uploadCvHandler;
    private readonly SubmitDiscResultCommandHandler _submitDiscHandler;
    private readonly ProcessCvAnalysisCommandHandler _processCvHandler;
    private readonly ProcessDiscInterpretationCommandHandler _processDiscHandler;
    private readonly GenerateInterviewReportCommandHandler _generateReportHandler;
    private readonly GetCandidateByIdQueryHandler _getCandidateByIdHandler;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IJobPositionRepository _jobPositionRepository;
    private readonly IBackgroundJobQueue? _backgroundJobQueue;

    public IngestCandidateCommandHandler(
        RegisterCandidateCommandHandler registerHandler,
        UploadCvCommandHandler uploadCvHandler,
        SubmitDiscResultCommandHandler submitDiscHandler,
        ProcessCvAnalysisCommandHandler processCvHandler,
        ProcessDiscInterpretationCommandHandler processDiscHandler,
        GenerateInterviewReportCommandHandler generateReportHandler,
        GetCandidateByIdQueryHandler getCandidateByIdHandler,
        ICandidateRepository candidateRepository,
        IJobPositionRepository jobPositionRepository,
        IBackgroundJobQueue? backgroundJobQueue = null)
    {
        _registerHandler = registerHandler;
        _uploadCvHandler = uploadCvHandler;
        _submitDiscHandler = submitDiscHandler;
        _processCvHandler = processCvHandler;
        _processDiscHandler = processDiscHandler;
        _generateReportHandler = generateReportHandler;
        _getCandidateByIdHandler = getCandidateByIdHandler;
        _candidateRepository = candidateRepository;
        _jobPositionRepository = jobPositionRepository;
        _backgroundJobQueue = backgroundJobQueue;
    }

    public async Task<Result<CandidateDto>> HandleAsync(IngestCandidateCommand command, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid();

        string? resolvedTargetRole = command.TargetRole;
        if (command.JobPositionId.HasValue)
        {
            var position = await _jobPositionRepository.GetByIdAsync(command.JobPositionId.Value, cancellationToken);
            if (position != null)
            {
                resolvedTargetRole ??= position.Title;
            }
        }

        // 1. Register candidate or retrieve if already exists
        Guid candidateId;
        var existingCandidate = await _candidateRepository.GetByEmailAsync(command.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (existingCandidate is not null)
        {
            candidateId = existingCandidate.Id;
            if (command.JobPositionId.HasValue || !string.IsNullOrWhiteSpace(resolvedTargetRole))
            {
                existingCandidate.AssignToJobPosition(command.JobPositionId, resolvedTargetRole);
                _candidateRepository.Update(existingCandidate);
            }
        }
        else
        {
            var registerResult = await _registerHandler.HandleAsync(
                new RegisterCandidateCommand(command.FirstName, command.LastName, command.Email, command.PhoneNumber),
                cancellationToken);

            if (registerResult.IsFailure)
            {
                return Result.Failure<CandidateDto>(registerResult.Error);
            }

            candidateId = registerResult.Value.Id;

            var createdCandidate = await _candidateRepository.GetByIdAsync(candidateId, cancellationToken);
            if (createdCandidate != null && (command.JobPositionId.HasValue || !string.IsNullOrWhiteSpace(resolvedTargetRole)))
            {
                createdCandidate.AssignToJobPosition(command.JobPositionId, resolvedTargetRole);
                _candidateRepository.Update(createdCandidate);
            }
        }

        // 2. Upload CV Document
        var uploadResult = await _uploadCvHandler.HandleAsync(
            new UploadCvCommand(candidateId, command.CvStream, command.FileName, command.FileSizeBytes, command.ContentType),
            cancellationToken);

        if (uploadResult.IsFailure)
        {
            return Result.Failure<CandidateDto>(uploadResult.Error);
        }

        var documentId = uploadResult.Value.DocumentId;

        // 3. Submit DISC Test Result
        var discResult = await _submitDiscHandler.HandleAsync(
            new SubmitDiscResultCommand(
                candidateId,
                command.Dominance,
                command.Influence,
                command.Steadiness,
                command.Conscientiousness,
                command.PrimaryStyle),
            cancellationToken);

        if (discResult.IsFailure)
        {
            return Result.Failure<CandidateDto>(discResult.Error);
        }

        var discResultId = discResult.Value;

        if (command.Asynchronous && _backgroundJobQueue != null)
        {
            _backgroundJobQueue.Enqueue(async (sp, ct) =>
            {
                var cvHandler = (ProcessCvAnalysisCommandHandler)sp.GetService(typeof(ProcessCvAnalysisCommandHandler))!;
                var discHandler = (ProcessDiscInterpretationCommandHandler)sp.GetService(typeof(ProcessDiscInterpretationCommandHandler))!;
                var reportHandler = (GenerateInterviewReportCommandHandler)sp.GetService(typeof(GenerateInterviewReportCommandHandler))!;

                await cvHandler.HandleAsync(
                    new ProcessCvAnalysisCommand(candidateId, documentId, Guid.NewGuid(), correlationId),
                    ct);

                await discHandler.HandleAsync(
                    new ProcessDiscInterpretationCommand(candidateId, discResultId, Guid.NewGuid(), correlationId),
                    ct);

                await reportHandler.HandleAsync(
                    new GenerateInterviewReportCommand(candidateId, Guid.NewGuid(), correlationId),
                    ct);
            });

            return await _getCandidateByIdHandler.HandleAsync(new GetCandidateByIdQuery(candidateId), cancellationToken);
        }

        // 4. Process CV Analysis with Gemini AI
        var cvAnalysisResult = await _processCvHandler.HandleAsync(
            new ProcessCvAnalysisCommand(candidateId, documentId, Guid.NewGuid(), correlationId),
            cancellationToken);

        if (cvAnalysisResult.IsFailure)
        {
            return Result.Failure<CandidateDto>(cvAnalysisResult.Error);
        }

        // 5. Process DISC Interpretation with Gemini AI
        var discInterpResult = await _processDiscHandler.HandleAsync(
            new ProcessDiscInterpretationCommand(candidateId, discResultId, Guid.NewGuid(), correlationId),
            cancellationToken);

        if (discInterpResult.IsFailure)
        {
            return Result.Failure<CandidateDto>(discInterpResult.Error);
        }

        // 6. Generate Pre-interview Report with Gemini AI
        var reportResult = await _generateReportHandler.HandleAsync(
            new GenerateInterviewReportCommand(candidateId, Guid.NewGuid(), correlationId),
            cancellationToken);

        if (reportResult.IsFailure)
        {
            return Result.Failure<CandidateDto>(reportResult.Error);
        }

        // 7. Return complete Dossier
        return await _getCandidateByIdHandler.HandleAsync(new GetCandidateByIdQuery(candidateId), cancellationToken);
    }
}

