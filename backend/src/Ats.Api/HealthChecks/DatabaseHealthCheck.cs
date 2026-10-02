using Ats.Application.Common.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ats.Api.HealthChecks;

public sealed class DatabaseHealthCheck(IDatabaseHealthProbe probe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await probe.IsAvailableAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("La base de datos no está disponible.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("No se pudo consultar el esquema de candidatos.", exception);
        }
    }
}
