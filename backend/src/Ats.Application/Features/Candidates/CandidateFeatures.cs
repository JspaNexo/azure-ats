using FluentValidation;
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
            .EmailAddress().WithMessage("El formato de correo no es válido.");
    }
}

public class RegisterCandidateCommandHandler
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCandidateCommandHandler(ICandidateRepository candidateRepository, IUnitOfWork unitOfWork)
    {
        _candidateRepository = candidateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CandidateDto>> HandleAsync(RegisterCandidateCommand command, CancellationToken cancellationToken = default)
    {
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

public class GetCandidateByIdQueryHandler
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICvAnalysisRepository _cvAnalysisRepository;
    private readonly IDiscRepository _discRepository;
    private readonly IInterviewReportRepository _reportRepository;

    public GetCandidateByIdQueryHandler(
        ICandidateRepository candidateRepository,
        ICvAnalysisRepository cvAnalysisRepository,
        IDiscRepository discRepository,
        IInterviewReportRepository reportRepository)
    {
        _candidateRepository = candidateRepository;
        _cvAnalysisRepository = cvAnalysisRepository;
        _discRepository = discRepository;
        _reportRepository = reportRepository;
    }

    public async Task<Result<CandidateDto>> HandleAsync(GetCandidateByIdQuery query, CancellationToken cancellationToken = default)
    {
        var candidate = await _candidateRepository.GetByIdAsync(query.Id, cancellationToken);
        if (candidate is null)
        {
            return Result.Failure<CandidateDto>(Error.NotFound("Candidate.NotFound", $"No se encontró el candidato con ID {query.Id}."));
        }

        var cvAnalysis = await _cvAnalysisRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var discInterp = await _discRepository.GetInterpretationByCandidateIdAsync(candidate.Id, cancellationToken);
        var discResult = await _discRepository.GetResultByCandidateIdAsync(candidate.Id, cancellationToken);
        var report = await _reportRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

        CvAnalysisDto? cvDto = null;
        if (cvAnalysis != null && !string.IsNullOrWhiteSpace(cvAnalysis.AnalysisJson) && cvAnalysis.AnalysisJson != "{}")
        {
            try { cvDto = System.Text.Json.JsonSerializer.Deserialize<CvAnalysisDto>(cvAnalysis.AnalysisJson); } catch { }
        }

        DiscInterpretationDto? discDto = null;
        if (discInterp != null && !string.IsNullOrWhiteSpace(discInterp.InterpretationJson) && discInterp.InterpretationJson != "{}")
        {
            try { discDto = System.Text.Json.JsonSerializer.Deserialize<DiscInterpretationDto>(discInterp.InterpretationJson); } catch { }
        }

        InterviewReportDto? reportDto = null;
        if (report != null && !string.IsNullOrWhiteSpace(report.ReportContentJson) && report.ReportContentJson != "{}")
        {
            try { reportDto = System.Text.Json.JsonSerializer.Deserialize<InterviewReportDto>(report.ReportContentJson); } catch { }
        }

        string targetRole = !string.IsNullOrWhiteSpace(candidate.TargetRole)
            ? candidate.TargetRole
            : (cvDto?.CurrentRole ?? "Senior Software Engineer");
        string seniority = cvDto?.EstimatedSeniority ?? "Senior";
        int expYears = (int)Math.Round(cvDto?.TotalExperienceYears ?? 5.0);
        string primaryStyle = discResult?.Scores.PrimaryStyle ?? discDto?.PrimaryStyle ?? "D/C";
        int matchScore = cvDto != null ? Math.Min(97, Math.Max(75, 76 + (expYears * 2) + (cvDto.Skills?.Count ?? 0))) : 88;
        string status = report != null && report.Status == Domain.Enums.ReportStatus.Generated ? "ReportReady" :
                        discInterp != null && discInterp.Status == Domain.Enums.ProcessingStatus.Processed ? "DiscEvaluated" :
                        cvAnalysis != null && cvAnalysis.Status == Domain.Enums.ProcessingStatus.Processed ? "CvAnalyzed" : "Registered";

        return Result.Success(new CandidateDto(
            candidate.Id,
            candidate.FirstName,
            candidate.LastName,
            candidate.Email.Value,
            candidate.PhoneNumber,
            candidate.CreatedAtUtc,
            TargetRole: targetRole,
            Seniority: seniority,
            ExperienceYears: expYears,
            MatchScore: matchScore,
            PrimaryDiscStyle: primaryStyle,
            Status: status,
            EvaluatorDecision: candidate.EvaluatorDecision,
            EvaluatorNotes: candidate.EvaluatorNotes,
            EvaluatedAtUtc: candidate.EvaluatedAtUtc,
            AssignedRecruiterId: candidate.AssignedRecruiterId,
            AssignedRecruiterName: candidate.AssignedRecruiterName,
            AssignedRecruiterEmail: candidate.AssignedRecruiterEmail,
            AssignedAtUtc: candidate.AssignedAtUtc,
            JobPositionId: candidate.JobPositionId,
            CvAnalysis: cvDto,
            DiscInterpretation: discDto,
            Report: reportDto));
    }
}

public record GetCandidatesQuery(string? RecruiterId = null);

public class GetCandidatesQueryHandler
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICvAnalysisRepository _cvAnalysisRepository;
    private readonly IDiscRepository _discRepository;
    private readonly IInterviewReportRepository _reportRepository;

    public GetCandidatesQueryHandler(
        ICandidateRepository candidateRepository,
        ICvAnalysisRepository cvAnalysisRepository,
        IDiscRepository discRepository,
        IInterviewReportRepository reportRepository)
    {
        _candidateRepository = candidateRepository;
        _cvAnalysisRepository = cvAnalysisRepository;
        _discRepository = discRepository;
        _reportRepository = reportRepository;
    }

    public async Task<Result<IReadOnlyList<CandidateDto>>> HandleAsync(GetCandidatesQuery query, CancellationToken cancellationToken = default)
    {
        var candidates = await _candidateRepository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(query.RecruiterId))
        {
            candidates = candidates.Where(c => c.AssignedRecruiterId == query.RecruiterId).ToList();
        }

        var dtos = new List<CandidateDto>();

        foreach (var c in candidates)
        {
            var cvAnalysis = await _cvAnalysisRepository.GetByCandidateIdAsync(c.Id, cancellationToken);
            var discInterp = await _discRepository.GetInterpretationByCandidateIdAsync(c.Id, cancellationToken);
            var discResult = await _discRepository.GetResultByCandidateIdAsync(c.Id, cancellationToken);
            var report = await _reportRepository.GetByCandidateIdAsync(c.Id, cancellationToken);

            CvAnalysisDto? cvDto = null;
            if (cvAnalysis != null && !string.IsNullOrWhiteSpace(cvAnalysis.AnalysisJson) && cvAnalysis.AnalysisJson != "{}")
            {
                try { cvDto = System.Text.Json.JsonSerializer.Deserialize<CvAnalysisDto>(cvAnalysis.AnalysisJson); } catch { }
            }

            DiscInterpretationDto? discDto = null;
            if (discInterp != null && !string.IsNullOrWhiteSpace(discInterp.InterpretationJson) && discInterp.InterpretationJson != "{}")
            {
                try { discDto = System.Text.Json.JsonSerializer.Deserialize<DiscInterpretationDto>(discInterp.InterpretationJson); } catch { }
            }

            InterviewReportDto? reportDto = null;
            if (report != null && !string.IsNullOrWhiteSpace(report.ReportContentJson) && report.ReportContentJson != "{}")
            {
                try { reportDto = System.Text.Json.JsonSerializer.Deserialize<InterviewReportDto>(report.ReportContentJson); } catch { }
            }

            string targetRole = !string.IsNullOrWhiteSpace(c.TargetRole)
                ? c.TargetRole
                : (cvDto?.CurrentRole ?? "Senior Software Engineer");
            string seniority = cvDto?.EstimatedSeniority ?? "Senior";
            int expYears = (int)Math.Round(cvDto?.TotalExperienceYears ?? 5.0);
            string primaryStyle = discResult?.Scores.PrimaryStyle ?? discDto?.PrimaryStyle ?? "D/C";
            int matchScore = cvDto != null ? Math.Min(97, Math.Max(75, 76 + (expYears * 2) + (cvDto.Skills?.Count ?? 0))) : 88;
            string status = report != null && report.Status == Domain.Enums.ReportStatus.Generated ? "ReportReady" :
                            discInterp != null && discInterp.Status == Domain.Enums.ProcessingStatus.Processed ? "DiscEvaluated" :
                            cvAnalysis != null && cvAnalysis.Status == Domain.Enums.ProcessingStatus.Processed ? "CvAnalyzed" : "Registered";

            dtos.Add(new CandidateDto(
                c.Id,
                c.FirstName,
                c.LastName,
                c.Email.Value,
                c.PhoneNumber,
                c.CreatedAtUtc,
                TargetRole: targetRole,
                Seniority: seniority,
                ExperienceYears: expYears,
                MatchScore: matchScore,
                PrimaryDiscStyle: primaryStyle,
                Status: status,
                EvaluatorDecision: c.EvaluatorDecision,
                EvaluatorNotes: c.EvaluatorNotes,
                EvaluatedAtUtc: c.EvaluatedAtUtc,
                AssignedRecruiterId: c.AssignedRecruiterId,
                AssignedRecruiterName: c.AssignedRecruiterName,
                AssignedRecruiterEmail: c.AssignedRecruiterEmail,
                AssignedAtUtc: c.AssignedAtUtc,
                JobPositionId: c.JobPositionId,
                CvAnalysis: cvDto,
                DiscInterpretation: discDto,
                Report: reportDto));
        }

        return Result.Success<IReadOnlyList<CandidateDto>>(dtos);
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

        candidate.SetEvaluatorDecision(command.Decision, command.Notes);
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


