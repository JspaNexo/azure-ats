using Ats.Domain.Entities;
using Ats.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ats.Tests;

public class PersistenceModelTests
{
    [Fact]
    public void EntityMappings_CanBuildWithoutConnectingToPostgres()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=test_ats;Username=ats_user;Password=ats_password")
            .Options;
        using var context = new ApplicationDbContext(options);

        // Accessing Model validates constructors and mappings; it does not open a connection.
        Assert.NotNull(context.Model.FindEntityType(typeof(Candidate)));
    }
}
