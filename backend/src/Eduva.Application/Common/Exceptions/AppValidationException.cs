using Eduva.Shared.Enums;
using FluentValidation.Results;

namespace Eduva.Application.Common.Exceptions
{
    public class AppValidationException : AppException
    {
        public AppValidationException(IEnumerable<ValidationFailure> failures)
            : base(
                CustomCode.ProvidedInformationIsInValid,
                failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}").ToList())
        {
        }
    }
}
