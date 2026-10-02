using Ats.Infrastructure.Services.Ai;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Ats.Tests;

public class GeminiConfigurationTests
{
    [Fact]
    public async Task MissingApiKey_ReturnsConfigurationErrorInsteadOfSimulatedQuestions()
    {
        using var client = new HttpClient();
        var provider = new GeminiAiProvider(client, Options.Create(new GeminiOptions()), NullLogger<GeminiAiProvider>.Instance);
        var result = await provider.GenerateQuestionsAsync(new(), new());
        Assert.True(result.IsFailure);
        Assert.Equal("Gemini.NotConfigured", result.Error.Code);
    }

    [Fact]
    public async Task Simulation_RequiresAnExplicitSetting()
    {
        using var client = new HttpClient();
        var provider = new GeminiAiProvider(client, Options.Create(new GeminiOptions { UseMockData = true }), NullLogger<GeminiAiProvider>.Instance);
        var result = await provider.GenerateQuestionsAsync(new(), new());
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.TechnicalQuestions);
        Assert.NotEmpty(result.Value.TechnicalQuestions);
    }
}
