using Ats.Infrastructure.Services.Security;
using Ats.Infrastructure.Services.Skills;
using Xunit;

namespace Ats.Tests;

public class CvAnalysisEnhancementTests
{
    private readonly SkillNormalizationService _skillNormalizer = new();

    [Theory]
    [InlineData("react", "React", "Frontend")]
    [InlineData("React.js", "React", "Frontend")]
    [InlineData("react js", "React", "Frontend")]
    [InlineData("c#", "C#", "Backend")]
    [InlineData("csharp", "C#", "Backend")]
    [InlineData(".net core", ".NET", "Backend")]
    [InlineData("postgresql", "PostgreSQL", "Database")]
    [InlineData("postgres", "PostgreSQL", "Database")]
    [InlineData("k8s", "Kubernetes", "DevOps")]
    [InlineData("amazon web services", "AWS", "Cloud")]
    [InlineData("sap fico", "SAP FI/CO", "Finance")]
    [InlineData("recruiting", "Reclutamiento y Selección", "Human Resources")]
    [InlineData("google ads", "SEM y Publicidad Digital", "Marketing")]
    [InlineData("b2b sales", "Ventas B2B", "Sales")]
    [InlineData("supply chain", "Cadena de Suministro", "Logistics & Operations")]
    public void SkillNormalization_ShouldResolveCanonicalNameAndCategory(string input, string expectedCanonical, string expectedCategory)
    {
        // Act
        var (canonical, category) = _skillNormalizer.Normalize(input);

        // Assert
        Assert.Equal(expectedCanonical, canonical);
        Assert.Equal(expectedCategory, category);
    }

    [Fact]
    public void SkillNormalization_WithUnknownSkill_ShouldPreserveCleanedName()
    {
        // Arrange
        string unknownSkill = "   CustomInternalToolFramework   ";

        // Act
        var (canonical, category) = _skillNormalizer.Normalize(unknownSkill);

        // Assert
        Assert.Equal("CustomInternalToolFramework", canonical);
        Assert.Equal("General", category);
    }

    [Fact]
    public void SkillNormalization_IsKnownSkill_ShouldIdentifyCatalogedSkills()
    {
        Assert.True(_skillNormalizer.IsKnownSkill("Docker"));
        Assert.True(_skillNormalizer.IsKnownSkill("docker"));
        Assert.False(_skillNormalizer.IsKnownSkill("UnknownTechnologyXYZ123"));
    }

    [Fact]
    public void AuthenticityDetector_WithCleanCv_ShouldReturnNoWarnings()
    {
        // Arrange
        string realisticCv = """
        Lucas Diaz - Ingeniero de Software
        Desarrollo de microservicios con C# y .NET 8.
        Optimizacion de consultas en PostgreSQL y despliegue en Kubernetes.
        Mantenimiento preventivo de colas con RabbitMQ.
        """;

        // Act
        var warnings = CvAuthenticityDetector.Detect(realisticCv);

        // Assert
        Assert.Empty(warnings);
    }

    [Fact]
    public void AuthenticityDetector_WithExcessiveBoilerplate_ShouldTriggerWarning()
    {
        // Arrange: contiene 3 o mas frases cliché detectables
        string aiClichéCv = """
        I am a results-driven professional with a proven track record of success.
        Always dynamic and detail-oriented, passionate about leveraging cutting-edge solutions
        and adept at problem-solving and thinking outside the box.
        """;

        // Act
        var warnings = CvAuthenticityDetector.Detect(aiClichéCv);

        // Assert
        Assert.NotEmpty(warnings);
        Assert.Contains(warnings, w => w.Contains("clichés o fórmulas genéricas"));
    }
}
