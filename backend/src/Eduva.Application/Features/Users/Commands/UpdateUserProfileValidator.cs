using FluentValidation;

namespace Eduva.Application.Features.Users.Commands
{
    public class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfileCommand>
    {
        public UpdateUserProfileValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^\d{10,15}$").WithMessage("Phone number must be between 10 and 15 digits.");

            RuleFor(x => x.AvatarUrl)
                .NotEmpty().WithMessage("Avatar URL is required.")
                .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("Avatar URL must be a valid absolute URL.");
        }
    }
}
