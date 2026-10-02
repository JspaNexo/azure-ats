using FluentValidation;
using Ats.Application.Common;
using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Domain.Common;
using Ats.Domain.Entities;
using Ats.Domain.ValueObjects;

namespace Ats.Application.Features.Candidates;

public record RegisterCandidateCommand(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber);

public class RegisterCandidateCommandValidator : AbstractValidator<RegisterCandidateCommand>
{
    public RegisterCandidateCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es obligatorio.")
            .MaximumLength(100).WithMessage("El apellido no puede exceder 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato de correo no es válido.").MaximumLength(255);
        RuleFor(x => x.PhoneNumber).MaximumLength(30);
    }
}

public class RegisterCandidateCommandHandler
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RegisterCandidateCommand> _validator;

    public RegisterCandidateCommandHandler(ICandidateRepository candidateRepository, IUnitOfWork unitOfWork, IValidator<RegisterCandidateCommand> validator)
    {
        _candidateRepository = candidateRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<CandidateDto>> HandleAsync(RegisterCandidateCommand command, CancellationToken cancellationToken = default)
    {
        var validationError = await _validator.ValidateCommandAsync(command, cancellationToken);
        if (validationError is not null) return Result.Failure<CandidateDto>(validationError);

        var emailResult = CandidateEmail.Create(command.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<CandidateDto>(emailResult.Error);
        }

        var existing = await _candidateRepository.GetByEmailAsync(emailResult.Value.Value, cancellationToken);
        if (existing is not null)
        {
            return Result.Failure<CandidateDto>(Error.Conflict("Candidate.EmailExists", "Ya existe un candidato registrado con este correo electrónico."));
        }

        var candidateResult = Candidate.Create(command.FirstName, command.LastName, emailResult.Value, command.PhoneNumber);
        if (candidateResult.IsFailure)
        {
            return Result.Failure<CandidateDto>(candidateResult.Error);
        }

        var candidate = candidateResult.Value;
        await _candidateRepository.AddAsync(candidate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CandidateDto(
            candidate.Id,
            candidate.FirstName,
            candidate.LastName,
            candidate.Email.Value,
            candidate.PhoneNumber,
            candidate.CreatedAtUtc));
    }
}

public record GetCandidateByIdQuery(Guid Id);

public sealed class GetCandidateByIdQueryHandler(ICandidateDetailsRepository repository)
{
    public async Task<Result<CandidateDto>> HandleAsync(GetCandidateByIdQuery query, CancellationToken cancellationToken = default)
    {
        var details = await repository.GetByIdAsync(query.Id, cancellationToken);
        return details is null
            ? Result.Failure<CandidateDto>(Error.NotFound("Candidate.NotFound", $"No se encontró el candidato con ID {query.Id}."))
            : Result.Success(CandidateDtoMapper.Map(details));
    }
}

public record GetCandidatesQuery(string? RecruiterId = null);

public sealed class GetCandidatesQueryHandler(ICandidateDetailsRepository repository)
{
    public async Task<Result<IReadOnlyList<CandidateDto>>> HandleAsync(GetCandidatesQuery query, CancellationToken cancellationToken = default)
    {
        var candidates = await repository.GetAllAsync(query.RecruiterId, cancellationToken);
        return Result.Success<IReadOnlyList<CandidateDto>>(candidates.Select(CandidateDtoMapper.Map).ToList());
    }
}

public record UpdateEvaluatorDecisionCommand(
    Guid CandidateId,
    string Decision,
    string? Notes);

public class UpdateEvaluatorDecisionCommandHandler
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly GetCandidateByIdQueryHandler _getByIdHandler;

    public UpdateEvaluatorDecisionCommandHandler(
        ICandidateRepository candidateRepository,
        IUnitOfWork unitOfWork,
        GetCandidateByIdQueryHandler getByIdHandler)
    {
        _candidateRepository = candidateRepository;
        _unitOfWork = unitOfWork;
        _getByIdHandler = getByIdHandler;
    }

    public async Task<Result<CandidateDto>> HandleAsync(UpdateEvaluatorDecisionCommand command, CancellationToken cancellationToken = default)
    {
        var candidate = await _candidateRepository.GetByIdAsync(command.CandidateId, cancellationToken);
        if (candidate is null)
        {
            return Result.Failure<CandidateDto>(Error.NotFound("Candidate.NotFound", $"Candidato con ID {command.CandidateId} no encontrado."));
        }

        var decisionResult = candidate.SetEvaluatorDecision(command.Decision, command.Notes);
        if (decisionResult.IsFailure) return Result.Failure<CandidateDto>(decisionResult.Error);
        _candidateRepository.Update(candidate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _getByIdHandler.HandleAsync(new GetCandidateByIdQuery(candidate.Id), cancellationToken);
    }
}

public record AssignCandidateCommand(
    Guid CandidateId,
    string? RecruiterId,
    string? RecruiterName,
    string? RecruiterEmail);

public class AssignCandidateCommandHandler
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly GetCandidateByIdQueryHandler _getByIdHandler;

    public AssignCandidateCommandHandler(
        ICandidateRepository candidateRepository,
        IUnitOfWork unitOfWork,
        GetCandidateByIdQueryHandler getByIdHandler)
    {
        _candidateRepository = candidateRepository;
        _unitOfWork = unitOfWork;
        _getByIdHandler = getByIdHandler;
    }

    public async Task<Result<CandidateDto>> HandleAsync(AssignCandidateCommand command, CancellationToken cancellationToken = default)
    {
        var candidate = await _candidateRepository.GetByIdAsync(command.CandidateId, cancellationToken);
        if (candidate is null)
        {
            return Result.Failure<CandidateDto>(Error.NotFound("Candidate.NotFound", $"Candidato con ID {command.CandidateId} no encontrado."));
        }

        candidate.AssignToRecruiter(command.RecruiterId, command.RecruiterName, command.RecruiterEmail);
        _candidateRepository.Update(candidate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _getByIdHandler.HandleAsync(new GetCandidateByIdQuery(candidate.Id), cancellationToken);
    }
}

public record GetRecruitersQuery;

public class GetRecruitersQueryHandler
{
    private readonly ICandidateRepository _candidateRepository;

    public GetRecruitersQueryHandler(ICandidateRepository candidateRepository)
    {
        _candidateRepository = candidateRepository;
    }

    public async Task<Result<IReadOnlyList<RecruiterDto>>> HandleAsync(GetRecruitersQuery query, CancellationToken cancellationToken = default)
    {
        var candidates = await _candidateRepository.GetAllAsync(cancellationToken);

        var recruiters = new List<RecruiterDto>
        {
            new("carlos.mendoza", "Carlos Mendoza", "carlos.mendoza@empresa.com", "Recruiter",
                candidates.Count(c => c.AssignedRecruiterId == "carlos.mendoza")),
            new("laura.sanchez", "Laura Sánchez", "laura.sanchez@empresa.com", "Recruiter",
                candidates.Count(c => c.AssignedRecruiterId == "laura.sanchez"))
        };

        return Result.Success<IReadOnlyList<RecruiterDto>>(recruiters);
    }
}

