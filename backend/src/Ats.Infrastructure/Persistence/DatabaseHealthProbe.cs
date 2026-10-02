using Ats.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ats.Infrastructure.Persistence;

public sealed class DatabaseHealthProbe(ApplicationDbContext context) : IDatabaseHealthProbe
{
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        // Execute a mapped query so a missing or incompatible schema is also detected.
        _ = await context.Candidates.AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync(cancellationToken);
        return true;
    }
}
