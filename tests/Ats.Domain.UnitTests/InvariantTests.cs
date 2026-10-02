using Ats.Domain.Entities;
using Ats.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Ats.Domain.UnitTests;

public class InvariantTests
{
    [Fact]
    public void InvalidDecision_DoesNotChangeTheCandidate()
    {
        var candidate = Candidate.Create("Ana", "Perez", CandidateEmail.Create("ana@example.com").Value).Value;
        candidate.SetEvaluatorDecision("Unknown", "notes").IsFailure.Should().BeTrue();
        candidate.EvaluatorDecision.Should().Be("Pending");
        candidate.EvaluatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void InvalidStatus_DoesNotReopenAClosedVacancy()
    {
        var position = JobPosition.Create("Developer", "IT").Value;
        position.UpdateStatus("Closed");
        position.UpdateStatus("Typo").IsFailure.Should().BeTrue();
        position.Status.Should().Be("Closed");
    }

    [Fact]
    public void Email_IsStoredInCanonicalForm()
    {
        CandidateEmail.Create(" Ana@EXAMPLE.com ").Value.Value.Should().Be("ana@example.com");
    }

    [Fact]
    public void InvalidDiscStyle_IsRejected()
    {
        DiscScores.Create(10, 20, 30, 40, new string('x', 20)).IsFailure.Should().BeTrue();
    }
}
