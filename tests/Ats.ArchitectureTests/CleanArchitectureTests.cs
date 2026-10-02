using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Ats.ArchitectureTests;

public class CleanArchitectureTests
{
    [Fact]
    public void Controllers_ShouldNotDependOnPersistenceOrRenderingServices()
    {
        var result = Types.InAssembly(typeof(Api.Controllers.CandidatesController).Assembly)
            .That().ResideInNamespace("Ats.Api.Controllers")
            .ShouldNot().HaveDependencyOnAny("Ats.Infrastructure", "Ats.Application.Common.Interfaces")
            .GetResult();
        result.IsSuccessful.Should().BeTrue("los controladores deben delegar los casos de uso a Application.");
    }
    private const string DomainNamespace = "Ats.Domain";
    private const string ApplicationNamespace = "Ats.Application";
    private const string InfrastructureNamespace = "Ats.Infrastructure";
    private const string ApiNamespace = "Ats.Api";

    [Fact]
    public void Domain_Should_Not_HaveDependencyOnOtherProjects()
    {
        // Arrange
        var assembly = typeof(Domain.Common.Entity<>).Assembly;

        var otherProjects = new[]
        {
            ApplicationNamespace,
            InfrastructureNamespace,
            ApiNamespace
        };

        // Act
        var testResult = Types
            .InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(otherProjects)
            .GetResult();

        // Assert
        testResult.IsSuccessful.Should().BeTrue("la capa de Dominio no debe depender de ninguna capa externa.");
    }

    [Fact]
    public void Application_Should_Not_HaveDependencyOnInfrastructureOrApi()
    {
        // Arrange
        var assembly = typeof(Application.DependencyInjection).Assembly;

        var otherProjects = new[]
        {
            InfrastructureNamespace,
            ApiNamespace
        };

        // Act
        var testResult = Types
            .InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(otherProjects)
            .GetResult();

        // Assert
        testResult.IsSuccessful.Should().BeTrue("la capa de Aplicación no debe depender de Infraestructura ni de la API.");
    }

    [Fact]
    public void Infrastructure_Should_Not_HaveDependencyOnApi()
    {
        // Arrange
        var assembly = typeof(Infrastructure.DependencyInjection).Assembly;

        // Act
        var testResult = Types
            .InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        // Assert
        testResult.IsSuccessful.Should().BeTrue("la capa de Infraestructura no debe depender de la API.");
    }

}


