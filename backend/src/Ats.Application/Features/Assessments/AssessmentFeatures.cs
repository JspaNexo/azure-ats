using System.Text.Json;
using FluentValidation;
using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Domain.Common;
using Ats.Domain.Entities;
using Ats.Domain.Enums;
using Ats.Domain.ValueObjects;

namespace Ats.Application.Features.Assessments;

public record SubmitAssessmentResultCommand(
    Guid CandidateId,
    string AssessmentType,
    Dictionary<string, double> Dimensions,
    string? PrimaryStyle = null,
    string? RawResultsJson = null);

public class SubmitAssessmentResultCommandValidator : AbstractValidator<SubmitAssessmentResultCommand>
{
    public SubmitAssessmentResultCommandValidator()
    {
        RuleFor(x => x.CandidateId).NotEmpty().WithMessage("El ID del candidato es obligatorio.");
        RuleFor(x => x.AssessmentType).NotEmpty().WithMessage("El tipo de evaluacion es obligatorio.");
        RuleFor(x => x.Dimensions)
            .NotNull().WithMessage("Las dimensiones no pueden ser nulas.")
            .Must(d => d != null && d.Count > 0).WithMessage("Debe proporcionar al menos una dimension puntuada.")
            .Must(d => d == null || d.Values.All(v => v >= 0 && v <= 100))
            .WithMessage("Todas las dimensiones deben estar en el rango de 0 a 100.");
    }
}

public class SubmitAssessmentResultCommandHandler
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICandidateAssessmentRepository _assessmentRepository;
    private readonly IDiscRepository _discRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitAssessmentResultCommandHandler(
        ICandidateRepository candidateRepository,
        ICandidateAssessmentRepository assessmentRepository,
        IDiscRepository discRepository,
        IUnitOfWork unitOfWork)
    {
        _candidateRepository = candidateRepository;
        _assessmentRepository = assessmentRepository;
        _discRepository = discRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> HandleAsync(SubmitAssessmentResultCommand command, CancellationToken cancellationToken = default)
    {
        var candidate = await _candidateRepository.GetByIdAsync(command.CandidateId, cancellationToken);
        if (candidate is null)
        {
            return Result.Failure<Guid>(Error.NotFound("Candidate.NotFound", $"Candidato {command.CandidateId} no encontrado."));
        }

        var scoresResult = AssessmentScores.Create(command.AssessmentType, command.Dimensions, command.PrimaryStyle);
        if (scoresResult.IsFailure)
        {
            return Result.Failure<Guid>(scoresResult.Error);
        }

        var assessmentResult = CandidateAssessment.Create(command.CandidateId, scoresResult.Value, command.RawResultsJson);
        if (assessmentResult.IsFailure)
        {
            return Result.Failure<Guid>(assessmentResult.Error);
        }

        await _assessmentRepository.AddResultAsync(assessmentResult.Value, cancellationToken);

        // Si es tipo DISC, tambien registrar en la tabla legacy de DISC para retrocompatibilidad
        if (string.Equals(command.AssessmentType, "DISC", StringComparison.OrdinalIgnoreCase))
        {
            var discScores = DiscScores.Create(
                scoresResult.Value.Dominance,
                scoresResult.Value.Influence,
                scoresResult.Value.Steadiness,
                scoresResult.Value.Conscientiousness,
                scoresResult.Value.PrimaryStyle);

            if (discScores.IsSuccess)
            {
                var discResult = DiscResult.Create(command.CandidateId, discScores.Value);
                if (discResult.IsSuccess)
                {
                    await _discRepository.AddResultAsync(discResult.Value, cancellationToken);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(assessmentResult.Value.Id);
    }
}

public record ProcessAssessmentInterpretationCommand(
    Guid CandidateId,
    Guid AssessmentId,
    Guid EventId,
    Guid CorrelationId);

public class ProcessAssessmentInterpretationCommandHandler
{
    private readonly ICandidateAssessmentRepository _assessmentRepository;
    private readonly IProcessingJobRepository _processingJobRepository;
    private readonly IAssessmentInterpreter _assessmentInterpreter;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessAssessmentInterpretationCommandHandler(
        ICandidateAssessmentRepository assessmentRepository,
        IProcessingJobRepository processingJobRepository,
        IAssessmentInterpreter assessmentInterpreter,
        IUnitOfWork unitOfWork)
    {
        _assessmentRepository = assessmentRepository;
        _processingJobRepository = processingJobRepository;
        _assessmentInterpreter = assessmentInterpreter;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AssessmentInterpretationDto>> HandleAsync(ProcessAssessmentInterpretationCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Check if candidate already has a processed interpretation (Cache / Token Guard)
        var existingInterpretation = await _assessmentRepository.GetInterpretationByCandidateIdAsync(command.CandidateId, cancellationToken);
        if (existingInterpretation is not null && existingInterpretation.Status == ProcessingStatus.Processed && !string.IsNullOrEmpty(existingInterpretation.InterpretationJson))
        {
            var cachedDto = JsonSerializer.Deserialize<AssessmentInterpretationDto>(existingInterpretation.InterpretationJson);
            if (cachedDto is not null) return Result.Success(cachedDto);
        }

        // 2. Idempotency check via ProcessingJob
        var existingJob = await _processingJobRepository.GetByEventIdAsync(command.EventId, cancellationToken);
        if (existingJob is not null && existingJob.Status == ProcessingStatus.Processed)
        {
            if (existingInterpretation is not null && !string.IsNullOrEmpty(existingInterpretation.InterpretationJson))
            {
                var cachedDto = JsonSerializer.Deserialize<AssessmentInterpretationDto>(existingInterpretation.InterpretationJson);
                if (cachedDto is not null) return Result.Success(cachedDto);
            }
        }

        var job = existingJob ?? ProcessingJob.Create(command.CandidateId, ProcessType.AssessmentInterpretation, command.EventId, command.CorrelationId);
        if (existingJob is null) await _processingJobRepository.AddAsync(job, cancellationToken);

        job.MarkAsProcessing();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Fetch Assessment result
        var assessment = await _assessmentRepository.GetResultByCandidateIdAsync(command.CandidateId, cancellationToken);
        if (assessment is null)
        {
            job.MarkAsFailed("Assessment.NotFound", $"Resultado de evaluacion del candidato {command.CandidateId} no encontrado.");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<AssessmentInterpretationDto>(Error.NotFound("Assessment.NotFound", "Resultado de evaluacion no encontrado."));
        }

        // 4. Interpret with AI
        var interpretationResult = await _assessmentInterpreter.InterpretAssessmentAsync(assessment.AssessmentType, assessment.Scores, cancellationToken);
        if (interpretationResult.IsFailure)
        {
            job.MarkAsFailed(interpretationResult.Error.Code, interpretationResult.Error.Description);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<AssessmentInterpretationDto>(interpretationResult.Error);
        }

        var assessmentInterpretation = await _assessmentRepository.GetInterpretationByCandidateIdAsync(command.CandidateId, cancellationToken)
            ?? AssessmentInterpretation.CreatePending(command.CandidateId, command.AssessmentId, assessment.AssessmentType, "GoogleGemini", "gemini-1.5-flash", "v1.0");

        string jsonContent = JsonSerializer.Serialize(interpretationResult.Value);
        assessmentInterpretation.MarkAsProcessed(jsonContent);

        if (assessmentInterpretation.Id == Guid.Empty || await _assessmentRepository.GetInterpretationByCandidateIdAsync(command.CandidateId, cancellationToken) is null)
        {
            await _assessmentRepository.AddInterpretationAsync(assessmentInterpretation, cancellationToken);
        }
        else
        {
            _assessmentRepository.UpdateInterpretation(assessmentInterpretation);
        }

        job.MarkAsProcessed();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(interpretationResult.Value);
    }
}

public record CandidateAssessmentDetailDto(
    Guid Id,
    Guid CandidateId,
    string AssessmentType,
    AssessmentScoresDto Scores,
    AssessmentInterpretationDto? Interpretation,
    DateTime CompletedAtUtc);

public record GetCandidateAssessmentQuery(Guid CandidateId);

public class GetCandidateAssessmentQueryHandler
{
    private readonly ICandidateAssessmentRepository _assessmentRepository;

    public GetCandidateAssessmentQueryHandler(ICandidateAssessmentRepository assessmentRepository)
    {
        _assessmentRepository = assessmentRepository;
    }

    public async Task<Result<CandidateAssessmentDetailDto>> HandleAsync(GetCandidateAssessmentQuery query, CancellationToken cancellationToken = default)
    {
        var assessment = await _assessmentRepository.GetResultByCandidateIdAsync(query.CandidateId, cancellationToken);
        if (assessment is null)
        {
            return Result.Failure<CandidateAssessmentDetailDto>(Error.NotFound("Assessment.NotFound", $"Evaluacion para el candidato {query.CandidateId} no encontrada."));
        }

        var interpretation = await _assessmentRepository.GetInterpretationByCandidateIdAsync(query.CandidateId, cancellationToken);
        AssessmentInterpretationDto? interpretationDto = null;
        if (interpretation is not null && !string.IsNullOrEmpty(interpretation.InterpretationJson))
        {
            try
            {
                interpretationDto = JsonSerializer.Deserialize<AssessmentInterpretationDto>(interpretation.InterpretationJson);
            }
            catch
            {
                // Si la deserializacion falla, se mantiene en null
            }
        }

        var scoresDto = new AssessmentScoresDto(
            assessment.Scores.AssessmentType,
            assessment.Scores.Dimensions,
            assessment.Scores.PrimaryStyle);

        var detail = new CandidateAssessmentDetailDto(
            assessment.Id,
            assessment.CandidateId,
            assessment.AssessmentType,
            scoresDto,
            interpretationDto,
            assessment.CompletedAtUtc);

        return Result.Success(detail);
    }
}
