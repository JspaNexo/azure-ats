using FluentValidation;
using Ats.Application.Common.Interfaces;
using Ats.Domain.Common;
using Ats.Domain.Entities;

namespace Ats.Application.Features.JobPositions;

public record JobPositionDto(
    Guid Id,
    string Title,
    string Department,
    string Seniority,
    int MinExperienceYears,
    string? Description,
    string? Requirements,
    string Status,
    DateTime CreatedAtUtc,
    int CandidateCount = 0);

public record CreateJobPositionCommand(
    string Title,
    string Department,
    string Seniority,
    int MinExperienceYears,
    string? Description,
    string? Requirements);

public class CreateJobPositionCommandValidator : AbstractValidator<CreateJobPositionCommand>
{
    public CreateJobPositionCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("El título de la vacante es obligatorio.").MaximumLength(150);
        RuleFor(x => x.Department).NotEmpty().WithMessage("El departamento es obligatorio.").MaximumLength(100);
        RuleFor(x => x.MinExperienceYears).GreaterThanOrEqualTo(0).WithMessage("Los años mínimos de experiencia no pueden ser negativos.");
    }
}

public class CreateJobPositionCommandHandler
{
    private readonly IJobPositionRepository _jobPositionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateJobPositionCommand>? _validator;

    public CreateJobPositionCommandHandler(
        IJobPositionRepository jobPositionRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateJobPositionCommand>? validator = null)
    {
        _jobPositionRepository = jobPositionRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<JobPositionDto>> HandleAsync(CreateJobPositionCommand command, CancellationToken cancellationToken = default)
    {
        if (_validator != null)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                var first = validationResult.Errors[0];
                return Result.Failure<JobPositionDto>(Error.Validation(first.PropertyName, first.ErrorMessage));
            }
        }

        var positionResult = JobPosition.Create(
            command.Title,
            command.Department,
            command.Seniority,
            command.MinExperienceYears,
            command.Description,
            command.Requirements);

        if (positionResult.IsFailure)
        {
            return Result.Failure<JobPositionDto>(positionResult.Error);
        }

        var position = positionResult.Value;
        await _jobPositionRepository.AddAsync(position, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new JobPositionDto(
            position.Id,
            position.Title,
            position.Department,
            position.Seniority,
            position.MinExperienceYears,
            position.Description,
            position.Requirements,
            position.Status,
            position.CreatedAtUtc,
            CandidateCount: 0));
    }
}

public record GetJobPositionsQuery(string? Status = null);

public class GetJobPositionsQueryHandler
{
    private readonly IJobPositionRepository _jobPositionRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICacheService? _cacheService;

    public GetJobPositionsQueryHandler(
        IJobPositionRepository jobPositionRepository,
        ICandidateRepository candidateRepository,
        ICacheService? cacheService = null)
    {
        _jobPositionRepository = jobPositionRepository;
        _candidateRepository = candidateRepository;
        _cacheService = cacheService;
    }

    public async Task<Result<IReadOnlyList<JobPositionDto>>> HandleAsync(GetJobPositionsQuery query, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"catalog_job_positions_{query.Status ?? "all"}";
        if (_cacheService != null)
        {
            var cached = _cacheService.Get<IReadOnlyList<JobPositionDto>>(cacheKey);
            if (cached != null)
            {
                return Result.Success(cached);
            }
        }

        var positions = await _jobPositionRepository.GetAllAsync(query.Status, cancellationToken);
        var candidates = await _candidateRepository.GetAllAsync(cancellationToken);

        var dtos = positions.Select(p => new JobPositionDto(
            p.Id,
            p.Title,
            p.Department,
            p.Seniority,
            p.MinExperienceYears,
            p.Description,
            p.Requirements,
            p.Status,
            p.CreatedAtUtc,
            CandidateCount: candidates.Count(c => c.JobPositionId == p.Id || string.Equals(c.TargetRole, p.Title, StringComparison.OrdinalIgnoreCase))
        )).ToList().AsReadOnly();

        if (_cacheService != null)
        {
            _cacheService.Set(cacheKey, (IReadOnlyList<JobPositionDto>)dtos, TimeSpan.FromSeconds(30));
        }

        return Result.Success<IReadOnlyList<JobPositionDto>>(dtos);
    }
}

public record UpdateJobPositionStatusCommand(Guid Id, string Status);

public class UpdateJobPositionStatusCommandHandler
{
    private readonly IJobPositionRepository _jobPositionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateJobPositionStatusCommandHandler(IJobPositionRepository jobPositionRepository, IUnitOfWork unitOfWork)
    {
        _jobPositionRepository = jobPositionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<JobPositionDto>> HandleAsync(UpdateJobPositionStatusCommand command, CancellationToken cancellationToken = default)
    {
        var position = await _jobPositionRepository.GetByIdAsync(command.Id, cancellationToken);
        if (position is null)
        {
            return Result.Failure<JobPositionDto>(Error.NotFound("JobPosition.NotFound", "Vacante no encontrada."));
        }

        position.UpdateStatus(command.Status);
        _jobPositionRepository.Update(position);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new JobPositionDto(
            position.Id,
            position.Title,
            position.Department,
            position.Seniority,
            position.MinExperienceYears,
            position.Description,
            position.Requirements,
            position.Status,
            position.CreatedAtUtc));
    }
}