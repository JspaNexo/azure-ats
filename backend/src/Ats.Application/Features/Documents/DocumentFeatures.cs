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

    public UploadCvCommandHandler(
        ICandidateRepository candidateRepository,
        IDocumentStorageService storageService,
        IUnitOfWork unitOfWork)
    {
        _candidateRepository = candidateRepository;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UploadCvResponse>> HandleAsync(UploadCvCommand command, CancellationToken cancellationToken = default)
    {
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

