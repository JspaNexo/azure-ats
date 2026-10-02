using System.Text.Json;
using FluentValidation;
using Ats.Application.Common;
using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Domain.Common;
using Ats.Domain.Entities;
using Ats.Domain.Enums;
using Ats.Domain.ValueObjects;

namespace Ats.Application.Features.Disc;

public record SubmitDiscResultCommand(
    Guid CandidateId,
    int Dominance,
    int Influence,
    int Steadiness,
    int Conscientiousness,
    string? PrimaryStyle);

public class SubmitDiscResultCommandValidator : AbstractValidator<SubmitDiscResultCommand>
{
    public SubmitDiscResultCommandValidator()
    {
        RuleFor(x => x.CandidateId).NotEmpty().WithMessage("El ID del candidato es obligatorio.");
        RuleFor(x => x.Dominance).InclusiveBetween(0, 100).WithMessage("D debe estar entre 0 y 100.");
        RuleFor(x => x.Influence).InclusiveBetween(0, 100).WithMessage("I debe estar entre 0 y 100.");
        RuleFor(x => x.Steadiness).InclusiveBetween(0, 100).WithMessage("S debe estar entre 0 y 100.");
        RuleFor(x => x.Conscientiousness).InclusiveBetween(0, 100).WithMessage("C debe estar entre 0 y 100.");
    }
}

public class SubmitDiscResultCommandHandler
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IDiscRepository _discRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<SubmitDiscResultCommand> _validator;

    public SubmitDiscResultCommandHandler(
        ICandidateRepository candidateRepository,
        IDiscRepository discRepository,
        IUnitOfWork unitOfWork,
        IValidator<SubmitDiscResultCommand> validator)
    {
        _candidateRepository = candidateRepository;
        _discRepository = discRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<Guid>> HandleAsync(SubmitDiscResultCommand command, CancellationToken cancellationToken = default)
    {
        var validationError = await _validator.ValidateCommandAsync(command, cancellationToken);
        if (validationError is not null) return Result.Failure<Guid>(validationError);

        var candidate = await _candidateRepository.GetByIdAsync(command.CandidateId, cancellationToken);
        if (candidate is null)
        {
            return Result.Failure<Guid>(Error.NotFound("Candidate.NotFound", $"Candidato {command.CandidateId} no encontrado."));
        }

        var scoresResult = DiscScores.Create(
            command.Dominance,
            command.Influence,
            command.Steadiness,
            command.Conscientiousness,
            command.PrimaryStyle);

        if (scoresResult.IsFailure)
        {
            return Result.Failure<Guid>(scoresResult.Error);
        }

        var discResultEntity = DiscResult.Create(command.CandidateId, scoresResult.Value);
        await _discRepository.AddResultAsync(discResultEntity.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(discResultEntity.Value.Id);
    }
}

public record ProcessDiscInterpretationCommand(
    Guid CandidateId,
    Guid DiscResultId,
    Guid EventId,
    Guid CorrelationId);

public class ProcessDiscInterpretationCommandHandler
{
    private readonly IDiscRepository _discRepository;
    private readonly IProcessingJobRepository _processingJobRepository;
    private readonly IDiscInterpreter _discInterpreter;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessDiscInterpretationCommandHandler(
        IDiscRepository discRepository,
        IProcessingJobRepository processingJobRepository,
        IDiscInterpreter discInterpreter,
        IUnitOfWork unitOfWork)
    {
        _discRepository = discRepository;
        _processingJobRepository = processingJobRepository;
        _discInterpreter = discInterpreter;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DiscInterpretationDto>> HandleAsync(ProcessDiscInterpretationCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Check if candidate already has a processed DISC interpretation (Cache / Token Guard)
        var existingInterpretation = await _discRepository.GetInterpretationByCandidateIdAsync(command.CandidateId, cancellationToken);
        if (existingInterpretation is not null && existingInterpretation.DiscResultId == command.DiscResultId && existingInterpretation.Status == ProcessingStatus.Processed && !string.IsNullOrEmpty(existingInterpretation.InterpretationJson))
        {
            var cachedDto = JsonSerializer.Deserialize<DiscInterpretationDto>(existingInterpretation.InterpretationJson);
            if (cachedDto is not null) return Result.Success(cachedDto);
        }

        // 2. Idempotency check via ProcessingJob
        var existingJob = await _processingJobRepository.GetByEventIdAsync(command.EventId, cancellationToken);
        if (existingJob is not null && (existingJob.CandidateId != command.CandidateId || existingJob.ProcessType != ProcessType.DiscInterpretation))
            return Result.Failure<DiscInterpretationDto>(Error.Conflict("Event.Mismatch", "El evento pertenece a otro proceso o candidato."));

        var discResult = await _discRepository.GetResultByIdAsync(command.DiscResultId, cancellationToken);
        if (discResult is null || discResult.CandidateId != command.CandidateId)
            return Result.Failure<DiscInterpretationDto>(Error.NotFound("Disc.NotFound", "Resultado DISC no encontrado para este candidato."));

        var job = existingJob ?? ProcessingJob.Create(command.CandidateId, ProcessType.DiscInterpretation, command.EventId, command.CorrelationId);
        if (existingJob is null) await _processingJobRepository.AddAsync(job, cancellationToken);
        job.MarkAsProcessing();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Interpret with AI
        var interpretationResult = await _discInterpreter.InterpretDiscAsync(discResult.Scores, cancellationToken);
        if (interpretationResult.IsFailure)
        {
            job.MarkAsFailed(interpretationResult.Error.Code, interpretationResult.Error.Description);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<DiscInterpretationDto>(interpretationResult.Error);
        }

        var discInterpretation = existingInterpretation?.DiscResultId == command.DiscResultId ? existingInterpretation
            : DiscInterpretation.CreatePending(command.CandidateId, command.DiscResultId, "GoogleGemini", "configured-model", "v1.0");

        string jsonContent = JsonSerializer.Serialize(interpretationResult.Value);
        discInterpretation.MarkAsProcessed(jsonContent);

        if (!ReferenceEquals(discInterpretation, existingInterpretation))
        {
            await _discRepository.AddInterpretationAsync(discInterpretation, cancellationToken);
        }
        else
        {
            _discRepository.UpdateInterpretation(discInterpretation);
        }

        job.MarkAsProcessed();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(interpretationResult.Value);
    }
}

