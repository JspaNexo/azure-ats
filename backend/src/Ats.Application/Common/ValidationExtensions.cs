using Ats.Domain.Common;
using FluentValidation;

namespace Ats.Application.Common;

internal static class ValidationExtensions
{
    public static async Task<Error?> ValidateCommandAsync<T>(this IValidator<T> validator, T command,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(command, cancellationToken);
        return result.IsValid ? null : Error.Validation("Request.Invalid",
            string.Join(" ", result.Errors.Select(error => error.ErrorMessage).Distinct()));
    }
}
