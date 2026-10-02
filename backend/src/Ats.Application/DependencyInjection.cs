using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Ats.Application.Features.Candidates;
using Ats.Application.Features.CvProcessing;
using Ats.Application.Features.Disc;
using Ats.Application.Features.Documents;
using Ats.Application.Features.Reports;
using Ats.Application.Features.Ingestion;
using Ats.Application.Features.JobPositions;

namespace Ats.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Handlers
        services.AddScoped<RegisterCandidateCommandHandler>();
        services.AddScoped<GetCandidateByIdQueryHandler>();
        services.AddScoped<GetCandidatesQueryHandler>();
        services.AddScoped<UploadCvCommandHandler>();
        services.AddScoped<ProcessCvAnalysisCommandHandler>();
        services.AddScoped<SubmitDiscResultCommandHandler>();
        services.AddScoped<ProcessDiscInterpretationCommandHandler>();
        services.AddScoped<GenerateInterviewReportCommandHandler>();
        services.AddScoped<GetInterviewReportQueryHandler>();
        services.AddScoped<DownloadInterviewReportQueryHandler>();
        services.AddScoped<IngestCandidateCommandHandler>();
        services.AddScoped<UpdateEvaluatorDecisionCommandHandler>();
        services.AddScoped<AssignCandidateCommandHandler>();
        services.AddScoped<GetRecruitersQueryHandler>();
        services.AddScoped<CreateJobPositionCommandHandler>();
        services.AddScoped<GetJobPositionsQueryHandler>();
        services.AddScoped<UpdateJobPositionStatusCommandHandler>();

        return services;
    }
}

