using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using FluentValidation;

namespace Eduva.Application.Common.Validations
{
    public static class SchoolValidationRules
    {
        public static IRuleBuilderOptions<T, string> SchoolNameValidation<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("School name is required.")
                .MaximumLength(255);
        }

        public static IRuleBuilderOptions<T, string> ContactEmailFormatValidation<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("Contact email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
        }

        public static IRuleBuilderOptions<T, string> ContactPhoneValidation<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("Phone number is required")
                .Matches(@"^((03|05|07|08|09)\d{8}|02\d{9})$").WithMessage("Invalid phone number format");
        }

        public static IRuleBuilderOptions<T, string?> AddressValidation<T>(this IRuleBuilder<T, string?> ruleBuilder)
        {
            return ruleBuilder.MaximumLength(255);
        }

        public static IRuleBuilderOptions<T, string?> WebsiteUrlValidation<T>(this IRuleBuilder<T, string?> ruleBuilder)
        {
            return ruleBuilder
                .MaximumLength(255)
                .Must(url => string.IsNullOrWhiteSpace(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("Website URL must be a valid absolute URL.");
        }

        public static async Task<bool> IsEmailUniqueForCreate(IUnitOfWork unitOfWork, string email)
        {
            var repo = unitOfWork.GetRepository<School, int>();
            return !await repo.ExistsAsync(s => s.ContactEmail == email);
        }

        public static async Task<bool> IsEmailUniqueForUpdate(IUnitOfWork unitOfWork, string email, int schoolId)
        {
            var repo = unitOfWork.GetRepository<School, int>();
            return !await repo.ExistsAsync(s => s.ContactEmail == email && s.Id != schoolId);
        }
    }
}