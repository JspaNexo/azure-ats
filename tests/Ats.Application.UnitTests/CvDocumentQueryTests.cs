using FluentAssertions;
using NSubstitute;
using Ats.Application.Common.Interfaces;
using Ats.Application.Features.Documents;
using Ats.Domain.Entities;
using Ats.Domain.ValueObjects;
using Xunit;

namespace Ats.Application.UnitTests;

public class CvDocumentQueryTests
{
    private readonly ICandidateRepository _candidateRepository = Substitute.For<ICandidateRepository>();
    private readonly IDocumentStorageService _storageService = Substitute.For<IDocumentStorageService>();

    [Fact]
    public async Task GetCandidateCvDocument_WhenCandidateAndDocumentExist_ShouldReturnStream()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        var email = CandidateEmail.Create("valentin@example.com").Value;
        var candidate = Candidate.Create("Valentín", "Torres", email).Value;
        var document = candidate.AddCvDocument("cv_valentin.pdf", "candidates/cv_valentin.pdf", 1024, "application/pdf");

        _candidateRepository.GetByIdAsync(candidateId, Arg.Any<CancellationToken>())
            .Returns(candidate);

        using var memoryStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        _storageService.GetFileAsync(document.StoragePath, Arg.Any<CancellationToken>())
            .Returns(memoryStream);

        var handler = new GetCandidateCvDocumentQueryHandler(_candidateRepository, _storageService);
        var query = new GetCandidateCvDocumentQuery(candidateId);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be("cv_valentin.pdf");
        result.Value.ContentType.Should().Be("application/pdf");
        result.Value.Stream.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCandidateCvDocument_WhenCandidateNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        _candidateRepository.GetByIdAsync(candidateId, Arg.Any<CancellationToken>())
            .Returns((Candidate?)null);

        var handler = new GetCandidateCvDocumentQueryHandler(_candidateRepository, _storageService);
        var query = new GetCandidateCvDocumentQuery(candidateId);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Candidate.NotFound");
    }

    [Fact]
    public async Task GetCandidateCvDocument_WhenCandidateHasNoDocuments_ShouldReturnNotFound()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        var email = CandidateEmail.Create("sin_cv@example.com").Value;
        var candidate = Candidate.Create("Sin", "Documento", email).Value;

        _candidateRepository.GetByIdAsync(candidateId, Arg.Any<CancellationToken>())
            .Returns(candidate);

        var handler = new GetCandidateCvDocumentQueryHandler(_candidateRepository, _storageService);
        var query = new GetCandidateCvDocumentQuery(candidateId);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("CvDocument.NotFound");
    }
}

