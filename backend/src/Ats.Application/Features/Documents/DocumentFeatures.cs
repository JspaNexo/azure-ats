using FluentValidation;
using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Domain.Common;

namespace Ats.Application.Features.Documents;

public record UploadCvCommand(
    Guid CandidateId,
    Stream FileStream,
    string FileName,
    long FileSizeBytes,
    string ContentType);

public class UploadCvCommandValidator : AbstractValidator<UploadCvCommand>
{
    private static readonly string[] AllowedExtensions = [".pdf"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public UploadCvCommandValidator()
    {
        RuleFor(x => x.CandidateId)
            .NotEmpty().WithMessage("El ID del candidato es obligatorio.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("El nombre del archivo es obligatorio.")
            .Must(HaveValidExtension).WithMessage("Solo se permiten archivos en formato PDF.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("El archivo no puede estar vacío.")
            .LessThanOrEqualTo(MaxFileSizeBytes).WithMessage("El archivo no puede exceder los 10 MB.");
    }

    private bool HaveValidExtension(string fileName)
    {
        string extension = Path.GetExtension(fileName);
        return AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}

public class UploadCvCommandHandler
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IDocumentStorageService _storageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UploadCvCommand>? _validator;

    public UploadCvCommandHandler(
        ICandidateRepository candidateRepository,
        IDocumentStorageService storageService,
        IUnitOfWork unitOfWork,
        IValidator<UploadCvCommand>? validator = null)
    {
        _candidateRepository = candidateRepository;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<UploadCvResponse>> HandleAsync(UploadCvCommand command, CancellationToken cancellationToken = default)
    {
        if (_validator != null)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                var first = validationResult.Errors[0];
                return Result.Failure<UploadCvResponse>(Error.Validation(first.PropertyName, first.ErrorMessage));
            }
        }

        var candidate = await _candidateRepository.GetByIdAsync(command.CandidateId, cancellationToken);
        if (candidate is null)
        {
            return Result.Failure<UploadCvResponse>(
                Error.NotFound("Candidate.NotFound", $"No se encontró el candidato con ID {command.CandidateId}."));
        }

        string storagePath = await _storageService.SaveFileAsync(
            command.FileStream,
            command.FileName,
            command.ContentType,
            cancellationToken);

        var document = candidate.AddCvDocument(
            command.FileName,
            storagePath,
            command.FileSizeBytes,
            command.ContentType);

        await _candidateRepository.AddCvDocumentAsync(document, cancellationToken);
        _candidateRepository.Update(candidate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new UploadCvResponse(
            document.Id,
            candidate.Id,
            document.FileName,
            document.FileSizeBytes,
            "UPLOADED"));
    }
}

public record CvDocumentFileDto(
    Stream Stream,
    string FileName,
    string ContentType,
    long FileSizeBytes);

public record GetCandidateCvDocumentQuery(Guid CandidateId);

public class GetCandidateCvDocumentQueryHandler
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IDocumentStorageService _storageService;

    public GetCandidateCvDocumentQueryHandler(
        ICandidateRepository candidateRepository,
        IDocumentStorageService storageService)
    {
        _candidateRepository = candidateRepository;
        _storageService = storageService;
    }

    public async Task<Result<CvDocumentFileDto>> HandleAsync(
        GetCandidateCvDocumentQuery query,
        CancellationToken cancellationToken = default)
    {
        var candidate = await _candidateRepository.GetByIdAsync(query.CandidateId, cancellationToken);
        if (candidate is null)
        {
            return Result.Failure<CvDocumentFileDto>(
                Error.NotFound("Candidate.NotFound", $"No se encontró el candidato con ID {query.CandidateId}."));
        }

        var document = candidate.Documents
            .OrderByDescending(d => d.UploadedAtUtc)
            .FirstOrDefault();

        if (document is null)
        {
            return Result.Failure<CvDocumentFileDto>(
                Error.NotFound("CvDocument.NotFound", $"El candidato {candidate.FirstName} {candidate.LastName} no tiene un documento curricular registrado."));
        }

        var stream = await _storageService.GetFileAsync(document.StoragePath, cancellationToken);
        if (stream is null)
        {
            return Result.Failure<CvDocumentFileDto>(
                Error.NotFound("CvDocument.FileNotFound", $"El archivo {document.FileName} no se encuentra disponible en el almacenamiento."));
        }

        return Result.Success(new CvDocumentFileDto(
            stream,
            document.FileName,
            string.IsNullOrWhiteSpace(document.ContentType) ? "application/pdf" : document.ContentType,
            document.FileSizeBytes));
    }
}

