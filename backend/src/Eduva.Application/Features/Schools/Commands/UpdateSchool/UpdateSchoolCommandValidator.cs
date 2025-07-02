using Eduva.Application.Common.Validations;
using Eduva.Application.Interfaces;
using FluentValidation;

namespace Eduva.Application.Features.Schools.Commands.UpdateSchool
{
    public class UpdateSchoolCommandValidator : AbstractValidator<UpdateSchoolCommand>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSchoolCommandValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            RuleFor(x => x.Name).SchoolNameValidation();

            RuleFor(x => x.ContactEmail)
                .ContactEmailFormatValidation()
                .MustAsync(EmailIsUnique).WithMessage("Email already exists.");

            RuleFor(x => x.ContactPhone).ContactPhoneValidation();
            RuleFor(x => x.Address).AddressValidation();
            RuleFor(x => x.WebsiteUrl).WebsiteUrlValidation();
        }

        private async Task<bool> EmailIsUnique(UpdateSchoolCommand command, string email, CancellationToken token)
        {
            return await SchoolValidationRules.IsEmailUniqueForUpdate(_unitOfWork, email, command.Id);
        }
    }
}