using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ats.Application.Common.Interfaces;
using Ats.Infrastructure.Persistence;
using Ats.Infrastructure.Persistence.Repositories;
using Ats.Infrastructure.Services.Ai;
using Ats.Infrastructure.Services.Pdf;
using Ats.Infrastructure.Services.Reporting;
using Ats.Infrastructure.Services.Storage;

namespace Ats.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Database Context
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=ats_db;Username=postgres;Password=postgres";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // 2. Repositories & UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDatabaseHealthProbe, DatabaseHealthProbe>();
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<ICandidateDetailsRepository, CandidateDetailsRepository>();
        services.AddScoped<ICvAnalysisRepository, CvAnalysisRepository>();
        services.AddScoped<IDiscRepository, DiscRepository>();
        services.AddScoped<IInterviewReportRepository, InterviewReportRepository>();
        services.AddScoped<IProcessingJobRepository, ProcessingJobRepository>();
        services.AddScoped<IJobPositionRepository, JobPositionRepository>();

        // 3. Document Extractor & Storage
        services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
        services.AddSingleton<IDocumentStorageService, LocalStorageService>();
        services.AddSingleton<IReportDocumentRenderer, ReportDocumentRenderer>();

        // 4. Gemini AI Provider with Resilience
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));

        services.AddHttpClient<GeminiAiProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(90);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
        });

        services.AddScoped<ICvAnalyzer>(sp => sp.GetRequiredService<GeminiAiProvider>());
        services.AddScoped<IDiscInterpreter>(sp => sp.GetRequiredService<GeminiAiProvider>());
        services.AddScoped<IInterviewQuestionGenerator>(sp => sp.GetRequiredService<GeminiAiProvider>());

        return services;
    }
}

