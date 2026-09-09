using System.Text.Json;
using FluentValidation;
using Ats.Application.Common.Interfaces;
using Ats.Application.DTOs;
using Ats.Domain.Common;
using Ats.Domain.Entities;
using Ats.Domain.ValueObjects;
using Ats.Application.Features.Scoring;

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
    private readonly ICandidateAssessmentRepository? _assessmentRepository;
    private readonly IInterviewReportRepository _reportRepository;
    private readonly IJobPositionRepository? _jobPositionRepository;
    private readonly JobFitScoringService? _jobFitScoringService;

    public GetCandidateByIdQueryHandler(
        ICandidateRepository candidateRepository,
        ICvAnalysisRepository cvAnalysisRepository,
        IDiscRepository discRepository,
        IInterviewReportRepository reportRepository,
        IJobPositionRepository? jobPositionRepository = null,
        JobFitScoringService? jobFitScoringService = null,
        ICandidateAssessmentRepository? assessmentRepository = null)
    {
        _candidateRepository = candidateRepository;
        _cvAnalysisRepository = cvAnalysisRepository;
        _discRepository = discRepository;
        _reportRepository = reportRepository;
        _jobPositionRepository = jobPositionRepository;
        _jobFitScoringService = jobFitScoringService;
        _assessmentRepository = assessmentRepository;
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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

        CandidateAssessment? assessment = null;
        AssessmentInterpretation? assessmentInterp = null;
        if (_assessmentRepository != null)
        {
            assessment = await _assessmentRepository.GetResultByCandidateIdAsync(candidate.Id, cancellationToken);
            assessmentInterp = await _assessmentRepository.GetInterpretationByCandidateIdAsync(candidate.Id, cancellationToken);
        }

        CvAnalysisDto? cvDto = null;
        if (cvAnalysis != null && !string.IsNullOrWhiteSpace(cvAnalysis.AnalysisJson) && cvAnalysis.AnalysisJson != "{}")
        {
            try { cvDto = System.Text.Json.JsonSerializer.Deserialize<CvAnalysisDto>(cvAnalysis.AnalysisJson, JsonOptions); } catch { }
        }

        DiscInterpretationDto? discDto = null;
        if (discInterp != null && !string.IsNullOrWhiteSpace(discInterp.InterpretationJson) && discInterp.InterpretationJson != "{}")
        {
            try { discDto = System.Text.Json.JsonSerializer.Deserialize<DiscInterpretationDto>(discInterp.InterpretationJson, JsonOptions); } catch { }
        }

        AssessmentInterpretationDto? assessmentDto = null;
        if (assessmentInterp != null && !string.IsNullOrWhiteSpace(assessmentInterp.InterpretationJson) && assessmentInterp.InterpretationJson != "{}")
        {
            try { assessmentDto = System.Text.Json.JsonSerializer.Deserialize<AssessmentInterpretationDto>(assessmentInterp.InterpretationJson, JsonOptions); } catch { }
        }

        InterviewReportDto? reportDto = null;
        if (report != null && !string.IsNullOrWhiteSpace(report.ReportContentJson) && report.ReportContentJson != "{}")
        {
            try { reportDto = System.Text.Json.JsonSerializer.Deserialize<InterviewReportDto>(report.ReportContentJson, JsonOptions); } catch { }
        }

        string targetRole = !string.IsNullOrWhiteSpace(candidate.TargetRole)
            ? candidate.TargetRole
            : (cvDto?.CurrentRole ?? "");
        string seniority = cvDto?.EstimatedSeniority ?? "";
        int expYears = cvDto != null ? (int)Math.Round(cvDto.TotalExperienceYears) : 0;

        string assessmentType = assessment?.AssessmentType ?? (discResult != null ? "DISC" : "");
        var dimensionScores = assessment?.Scores.Dimensions.ToDictionary(k => k.Key, v => v.Value)
            ?? (discResult != null ? new Dictionary<string, double> {
                ["Dominance"] = discResult.Scores.Dominance,
                ["Influence"] = discResult.Scores.Influence,
                ["Steadiness"] = discResult.Scores.Steadiness,
                ["Conscientiousness"] = discResult.Scores.Conscientiousness
            } : null);

        string primaryStyle = assessment?.Scores.PrimaryStyle ?? discResult?.Scores.PrimaryStyle ?? discDto?.PrimaryStyle ?? "";

        // Calculate dynamic Job Fit Score
        JobFitResult? jobFit = null;
        if (cvDto != null && _jobFitScoringService != null)
        {
            JobPosition? position = null;
            if (candidate.JobPositionId.HasValue && _jobPositionRepository != null)
            {
                position = await _jobPositionRepository.GetByIdAsync(candidate.JobPositionId.Value, cancellationToken);
            }
            if (position == null && !string.IsNullOrWhiteSpace(candidate.TargetRole) && _jobPositionRepository != null)
            {
                var allPositions = await _jobPositionRepository.GetAllAsync(status: null, cancellationToken: cancellationToken);
                position = allPositions.FirstOrDefault(p =>
                    string.Equals(p.Title, candidate.TargetRole, StringComparison.OrdinalIgnoreCase) ||
                    candidate.TargetRole.Contains(p.Title, StringComparison.OrdinalIgnoreCase) ||
                    p.Title.Contains(candidate.TargetRole, StringComparison.OrdinalIgnoreCase));
            }
            jobFit = _jobFitScoringService.CalculateFit(cvDto, position);
        }

        int matchScore = jobFit?.OverallScore ?? (cvDto != null ? _jobFitScoringService?.CalculateFit(cvDto, null).OverallScore ?? 0 : 0);
        string status = report != null && report.Status == Domain.Enums.ReportStatus.Generated ? "ReportReady" :
                        (assessmentInterp != null && assessmentInterp.Status == Domain.Enums.ProcessingStatus.Processed) || (discInterp != null && discInterp.Status == Domain.Enums.ProcessingStatus.Processed) ? "DiscEvaluated" :
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
            Report: reportDto,
            JobFitDetail: jobFit,
            AssessmentType: assessmentType,
            AssessmentScores: dimensionScores,
            AssessmentInterpretation: assessmentDto));
    }
}

public record GetCandidatesQuery(string? RecruiterId = null);

public class GetCandidatesQueryHandler
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICvAnalysisRepository _cvAnalysisRepository;
    private readonly IDiscRepository _discRepository;
    private readonly ICandidateAssessmentRepository? _assessmentRepository;
    private readonly IInterviewReportRepository _reportRepository;
    private readonly IJobPositionRepository? _jobPositionRepository;
    private readonly JobFitScoringService? _jobFitScoringService;

    public GetCandidatesQueryHandler(
        ICandidateRepository candidateRepository,
        ICvAnalysisRepository cvAnalysisRepository,
        IDiscRepository discRepository,
        IInterviewReportRepository reportRepository,
        IJobPositionRepository? jobPositionRepository = null,
        JobFitScoringService? jobFitScoringService = null,
        ICandidateAssessmentRepository? assessmentRepository = null)
    {
        _candidateRepository = candidateRepository;
        _cvAnalysisRepository = cvAnalysisRepository;
        _discRepository = discRepository;
        _reportRepository = reportRepository;
        _jobPositionRepository = jobPositionRepository;
        _jobFitScoringService = jobFitScoringService;
        _assessmentRepository = assessmentRepository;
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<IReadOnlyList<CandidateDto>>> HandleAsync(GetCandidatesQuery query, CancellationToken cancellationToken = default)
    {
        var candidates = await _candidateRepository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(query.RecruiterId))
        {
            candidates = candidates.Where(c => c.AssignedRecruiterId == query.RecruiterId).ToList();
        }

        if (!candidates.Any())
        {
            return Result.Success<IReadOnlyList<CandidateDto>>(Array.Empty<CandidateDto>());
        }

        var candidateIds = candidates.Select(c => c.Id).ToList();

        var cvAnalyses = await _cvAnalysisRepository.GetByCandidateIdsAsync(candidateIds, cancellationToken);
        var discInterps = await _discRepository.GetInterpretationsByCandidateIdsAsync(candidateIds, cancellationToken);
        var discResults = await _discRepository.GetResultsByCandidateIdsAsync(candidateIds, cancellationToken);
        var reports = await _reportRepository.GetByCandidateIdsAsync(candidateIds, cancellationToken);

        IReadOnlyList<CandidateAssessment> assessments = Array.Empty<CandidateAssessment>();
        IReadOnlyList<AssessmentInterpretation> assessmentInterps = Array.Empty<AssessmentInterpretation>();
        if (_assessmentRepository != null)
        {
            assessments = await _assessmentRepository.GetResultsByCandidateIdsAsync(candidateIds, cancellationToken);
            assessmentInterps = await _assessmentRepository.GetInterpretationsByCandidateIdsAsync(candidateIds, cancellationToken);
        }

        var cvAnalysesDict = cvAnalyses.ToDictionary(a => a.CandidateId);
        var discInterpsDict = discInterps.ToDictionary(d => d.CandidateId);
        var discResultsDict = discResults.ToDictionary(r => r.CandidateId);
        var reportsDict = reports.ToDictionary(r => r.CandidateId);
        var assessmentsDict = assessments.ToDictionary(a => a.CandidateId);
        var assessmentInterpsDict = assessmentInterps.ToDictionary(i => i.CandidateId);

        IReadOnlyList<JobPosition> allJobPositions = _jobPositionRepository != null
            ? await _jobPositionRepository.GetAllAsync(status: null, cancellationToken: cancellationToken)
            : Array.Empty<JobPosition>();
        var jobPositionsDict = allJobPositions.ToDictionary(j => j.Id);

        var dtos = new List<CandidateDto>(candidates.Count);

        foreach (var c in candidates)
        {
            cvAnalysesDict.TryGetValue(c.Id, out var cvAnalysis);
            discInterpsDict.TryGetValue(c.Id, out var discInterp);
            discResultsDict.TryGetValue(c.Id, out var discResult);
            reportsDict.TryGetValue(c.Id, out var report);
            assessmentsDict.TryGetValue(c.Id, out var assessment);
            assessmentInterpsDict.TryGetValue(c.Id, out var assessmentInterp);

            CvAnalysisDto? cvDto = null;
            if (cvAnalysis != null && !string.IsNullOrWhiteSpace(cvAnalysis.AnalysisJson) && cvAnalysis.AnalysisJson != "{}")
            {
                try { cvDto = System.Text.Json.JsonSerializer.Deserialize<CvAnalysisDto>(cvAnalysis.AnalysisJson, JsonOptions); } catch { }
            }

            DiscInterpretationDto? discDto = null;
            if (discInterp != null && !string.IsNullOrWhiteSpace(discInterp.InterpretationJson) && discInterp.InterpretationJson != "{}")
            {
                try { discDto = System.Text.Json.JsonSerializer.Deserialize<DiscInterpretationDto>(discInterp.InterpretationJson, JsonOptions); } catch { }
            }

            AssessmentInterpretationDto? assessmentDto = null;
            if (assessmentInterp != null && !string.IsNullOrWhiteSpace(assessmentInterp.InterpretationJson) && assessmentInterp.InterpretationJson != "{}")
            {
                try { assessmentDto = System.Text.Json.JsonSerializer.Deserialize<AssessmentInterpretationDto>(assessmentInterp.InterpretationJson, JsonOptions); } catch { }
            }

            InterviewReportDto? reportDto = null;
            if (report != null && !string.IsNullOrWhiteSpace(report.ReportContentJson) && report.ReportContentJson != "{}")
            {
                try { reportDto = System.Text.Json.JsonSerializer.Deserialize<InterviewReportDto>(report.ReportContentJson, JsonOptions); } catch { }
            }

            string targetRole = !string.IsNullOrWhiteSpace(c.TargetRole)
                ? c.TargetRole
                : (cvDto?.CurrentRole ?? "");
            string seniority = cvDto?.EstimatedSeniority ?? "";
            int expYears = cvDto != null ? (int)Math.Round(cvDto.TotalExperienceYears) : 0;

            string assessmentType = assessment?.AssessmentType ?? (discResult != null ? "DISC" : "");
            var dimensionScores = assessment?.Scores.Dimensions.ToDictionary(k => k.Key, v => v.Value)
                ?? (discResult != null ? new Dictionary<string, double> {
                    ["Dominance"] = discResult.Scores.Dominance,
                    ["Influence"] = discResult.Scores.Influence,
                    ["Steadiness"] = discResult.Scores.Steadiness,
                    ["Conscientiousness"] = discResult.Scores.Conscientiousness
                } : null);

            string primaryStyle = assessment?.Scores.PrimaryStyle ?? discResult?.Scores.PrimaryStyle ?? discDto?.PrimaryStyle ?? "";

            // Calculate dynamic Job Fit Score
            JobFitResult? jobFit = null;
            if (cvDto != null && _jobFitScoringService != null)
            {
                JobPosition? position = null;
                if (c.JobPositionId.HasValue && jobPositionsDict.TryGetValue(c.JobPositionId.Value, out var foundPosition))
                {
                    position = foundPosition;
                }
                if (position == null && !string.IsNullOrWhiteSpace(c.TargetRole))
                {
                    position = allJobPositions.FirstOrDefault(p =>
                        string.Equals(p.Title, c.TargetRole, StringComparison.OrdinalIgnoreCase) ||
                        c.TargetRole.Contains(p.Title, StringComparison.OrdinalIgnoreCase) ||
                        p.Title.Contains(c.TargetRole, StringComparison.OrdinalIgnoreCase));
                }
                jobFit = _jobFitScoringService.CalculateFit(cvDto, position);
            }

            int matchScore = jobFit?.OverallScore ?? (cvDto != null ? _jobFitScoringService?.CalculateFit(cvDto, null).OverallScore ?? 0 : 0);
            string status = report != null && report.Status == Domain.Enums.ReportStatus.Generated ? "ReportReady" :
                            (assessmentInterp != null && assessmentInterp.Status == Domain.Enums.ProcessingStatus.Processed) || (discInterp != null && discInterp.Status == Domain.Enums.ProcessingStatus.Processed) ? "DiscEvaluated" :
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
                Report: reportDto,
                JobFitDetail: jobFit,
                AssessmentType: assessmentType,
                AssessmentScores: dimensionScores,
                AssessmentInterpretation: assessmentDto));
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
    private readonly ICacheService? _cacheService;

    public GetRecruitersQueryHandler(
        ICandidateRepository candidateRepository,
        ICacheService? cacheService = null)
    {
        _candidateRepository = candidateRepository;
        _cacheService = cacheService;
    }

    public async Task<Result<IReadOnlyList<RecruiterDto>>> HandleAsync(GetRecruitersQuery query, CancellationToken cancellationToken = default)
    {
        const string cacheKey = "catalog_recruiters";
        if (_cacheService != null)
        {
            var cached = _cacheService.Get<IReadOnlyList<RecruiterDto>>(cacheKey);
            if (cached != null)
            {
                return Result.Success(cached);
            }
        }

        var candidates = await _candidateRepository.GetAllAsync(cancellationToken);

        var recruiterMap = new Dictionary<string, (string Name, string Email)>(StringComparer.OrdinalIgnoreCase);

        // Evaluadores activos en expedientes asignados
        foreach (var c in candidates.Where(c => !string.IsNullOrWhiteSpace(c.AssignedRecruiterId)))
        {
            string recId = c.AssignedRecruiterId!;
            if (!recruiterMap.ContainsKey(recId))
            {
                recruiterMap[recId] = (
                    c.AssignedRecruiterName ?? recId,
                    c.AssignedRecruiterEmail ?? $"{recId.ToLowerInvariant()}@empresa.com"
                );
            }
        }

        // Si aún no hay asignaciones en base de datos, incluir evaluadores predeterminados de la organización
        if (!recruiterMap.ContainsKey("carlos.mendoza"))
        {
            recruiterMap["carlos.mendoza"] = ("Carlos Mendoza", "carlos.mendoza@empresa.com");
        }
        if (!recruiterMap.ContainsKey("laura.sanchez"))
        {
            recruiterMap["laura.sanchez"] = ("Laura Sánchez", "laura.sanchez@empresa.com");
        }

        var recruiters = recruiterMap.Select(kvp => new RecruiterDto(
            kvp.Key,
            kvp.Value.Name,
            kvp.Value.Email,
            "Recruiter",
            candidates.Count(c => string.Equals(c.AssignedRecruiterId, kvp.Key, StringComparison.OrdinalIgnoreCase))
        )).OrderBy(r => r.FullName).ToList().AsReadOnly();

        if (_cacheService != null)
        {
            _cacheService.Set(cacheKey, (IReadOnlyList<RecruiterDto>)recruiters, TimeSpan.FromSeconds(30));
        }

        return Result.Success<IReadOnlyList<RecruiterDto>>(recruiters);
    }
}


