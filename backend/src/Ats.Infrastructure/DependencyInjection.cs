using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ats.Application.Common.Interfaces;
using Ats.Infrastructure.Persistence;
using Ats.Infrastructure.Persistence.Repositories;
using Ats.Infrastructure.Services.Ai;
using Ats.Infrastructure.Services.Pdf;
using Ats.Infrastructure.Services.Reporting;
using Ats.Infrastructure.Services.Skills;
using Ats.Infrastructure.Services.Storage;
using Ats.Infrastructure.Services.Caching;
using Ats.Infrastructure.Services.Jobs;

namespace Ats.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Database Context
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // 2. Repositories & UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<ICvAnalysisRepository, CvAnalysisRepository>();
        services.AddScoped<IDiscRepository, DiscRepository>();
        services.AddScoped<IInterviewReportRepository, InterviewReportRepository>();
        services.AddScoped<IProcessingJobRepository, ProcessingJobRepository>();
        services.AddScoped<IJobPositionRepository, JobPositionRepository>();

        // 3. Document Extractor, Storage & Skill Normalizer
        services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
        
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        var storageProvider = configuration[$"{StorageOptions.SectionName}:Provider"] ?? "Local";
        if (string.Equals(storageProvider, "S3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IDocumentStorageService, S3StorageService>();
        }
        else
        {
            services.AddSingleton<IDocumentStorageService, LocalStorageService>();
        }

        services.AddSingleton<IReportDocumentRenderer, ReportDocumentRenderer>();
        services.AddSingleton<ISkillNormalizationService, SkillNormalizationService>();
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // 3.1. Background Processing Queue
        services.AddSingleton<ChannelBackgroundJobQueue>();
        services.AddSingleton<IBackgroundJobQueue>(sp => sp.GetRequiredService<ChannelBackgroundJobQueue>());
        services.AddHostedService<QueuedHostedService>();

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

        if (string.IsNullOrWhiteSpace(configuration["Gemini:ApiKey"]))
        {
            services.AddSingleton<MockAiProvider>();
            services.AddSingleton<ICvAnalyzer>(sp => sp.GetRequiredService<MockAiProvider>());
            services.AddSingleton<IDiscInterpreter>(sp => sp.GetRequiredService<MockAiProvider>());
            services.AddSingleton<IInterviewQuestionGenerator>(sp => sp.GetRequiredService<MockAiProvider>());
        }
        else
        {
            services.AddScoped<ICvAnalyzer>(sp => sp.GetRequiredService<GeminiAiProvider>());
            services.AddScoped<IDiscInterpreter>(sp => sp.GetRequiredService<GeminiAiProvider>());
            services.AddScoped<IInterviewQuestionGenerator>(sp => sp.GetRequiredService<GeminiAiProvider>());
        }

        return services;
    }
}

