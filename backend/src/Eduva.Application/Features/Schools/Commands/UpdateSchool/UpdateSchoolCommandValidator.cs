using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using FluentValidation;

namespace Eduva.Application.Features.Schools.Commands.UpdateSchool
{
    public class UpdateSchoolCommandValidator : AbstractValidator<UpdateSchoolCommand>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSchoolCommandValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("School name is required.")
                .MaximumLength(255);

            RuleFor(x => x.ContactEmail)
                .NotEmpty().WithMessage("Contact email is required.")
                .EmailAddress().WithMessage("Invalid email format.")
                .MustAsync(EmailIsUnique).WithMessage("Email already exists.");

            RuleFor(x => x.ContactPhone)
                .NotEmpty().WithMessage("Phone number is required")
                .Matches(@"^((03|05|07|08|09)\d{8}|02\d{9})$").WithMessage("Invalid phone number format");

            RuleFor(x => x.Address)
                .MaximumLength(255);

            RuleFor(x => x.WebsiteUrl)
                .MaximumLength(255)
                .Must(url => string.IsNullOrWhiteSpace(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("Website URL must be a valid absolute URL.");
        }

        private async Task<bool> EmailIsUnique(UpdateSchoolCommand command, string email, CancellationToken token)
        {
            var repo = _unitOfWork.GetRepository<School, int>();
            return !await repo.ExistsAsync(s => s.ContactEmail == email && s.Id != command.Id);
        }
    }
}