using FluentValidation;
using SiteInspectionTracker.Application.Common.Exceptions;

namespace SiteInspectionTracker.Application.Common.Validation;

public static class ValidatorExtensions
{
    /// <summary>
    /// Validates <paramref name="instance"/> and throws <see cref="RequestValidationException"/>
    /// with all failures grouped by property name when it is invalid.
    /// </summary>
    public static async Task EnsureValidAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken).ConfigureAwait(false);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        throw new RequestValidationException(errors);
    }
}
