namespace Ats.Application.Common.Interfaces;

public interface IDatabaseHealthProbe
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
