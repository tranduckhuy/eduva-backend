using Eduva.Shared.Enums;
using FluentValidation.Results;

namespace Eduva.Application.Common.Exceptions
{
    public class AppValidationException : AppException
    {
        //public IReadOnlyDictionary<string, string[]> Errors { get; }

        public AppValidationException(IEnumerable<ValidationFailure> failures)
            : base(
                CustomCode.ProvidedInformationIsInValid,
                failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}").ToList())
        {
            //Errors = failures
            //    .GroupBy(e => e.PropertyName)
            //    .ToDictionary(
            //        g => g.Key,
            //        g => g.Select(e => e.ErrorMessage).ToArray());
        }
    }
}
